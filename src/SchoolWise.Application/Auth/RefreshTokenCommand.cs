using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Application.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokensDto>>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(command => command.RefreshToken).NotEmpty().MaximumLength(512);
}

public sealed class RefreshTokenCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens,
    AutoMapper.IMapper mapper,
    IUnitOfWork unitOfWork) : IRequestHandler<RefreshTokenCommand, Result<AuthTokensDto>>
{
    public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var oldHash = tokens.HashRefreshToken(request.RefreshToken);
        var storedToken = await refreshTokens.GetByHashAsync(oldHash, cancellationToken);
        if (storedToken is null || !storedToken.IsActive(DateTimeOffset.UtcNow))
        {
            return Result<AuthTokensDto>.Failure("invalid_refresh_token", "The refresh token is invalid or expired.");
        }

        var user = await users.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            storedToken.Revoke();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<AuthTokensDto>.Failure("invalid_refresh_token", "The refresh token is invalid or expired.");
        }

        var clearReplacement = tokens.CreateRefreshToken();
        var replacementHash = tokens.HashRefreshToken(clearReplacement);
        storedToken.Revoke(replacementHash);
        await refreshTokens.AddAsync(
            new RefreshToken(user.Id, replacementHash, tokens.RefreshTokenExpiresAt),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthTokensDto>.Success(new AuthTokensDto(
            tokens.CreateAccessToken(user), tokens.AccessTokenExpiresAt, clearReplacement, tokens.RefreshTokenExpiresAt, mapper.Map<UserDto>(user)));
    }
}
