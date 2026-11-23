using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;

namespace SchoolWise.Application.Auth;

public sealed record RevokeRefreshTokenCommand(Guid UserId, string RefreshToken) : IRequest<Result<bool>>;

public sealed class RevokeRefreshTokenCommandValidator : AbstractValidator<RevokeRefreshTokenCommand>
{
    public RevokeRefreshTokenCommandValidator() => RuleFor(command => command.RefreshToken).NotEmpty().MaximumLength(512);
}

public sealed class RevokeRefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens,
    IUnitOfWork unitOfWork) : IRequestHandler<RevokeRefreshTokenCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await refreshTokens.GetByHashAsync(tokens.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (token is null || token.UserId != request.UserId || !token.IsActive(DateTimeOffset.UtcNow))
        {
            return Result<bool>.Failure("invalid_refresh_token", "The refresh token is invalid or expired.");
        }

        token.Revoke();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
