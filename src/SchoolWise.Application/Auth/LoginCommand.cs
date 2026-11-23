using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;

namespace SchoolWise.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthTokensDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class LoginCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordService passwords,
    ITokenService tokens,
    AutoMapper.IMapper mapper,
    IUnitOfWork unitOfWork) : IRequestHandler<LoginCommand, Result<AuthTokensDto>>
{
    public async Task<Result<AuthTokensDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive || !passwords.Verify(user, request.Password))
        {
            return Result<AuthTokensDto>.Failure("invalid_credentials", "Email or password is incorrect.");
        }

        var refresh = tokens.CreateRefreshToken();
        await refreshTokens.AddAsync(
            new Domain.Entities.RefreshToken(user.Id, tokens.HashRefreshToken(refresh), tokens.RefreshTokenExpiresAt),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthTokensDto>.Success(new AuthTokensDto(
            tokens.CreateAccessToken(user), tokens.AccessTokenExpiresAt, refresh, tokens.RefreshTokenExpiresAt, mapper.Map<UserDto>(user)));
    }
}
