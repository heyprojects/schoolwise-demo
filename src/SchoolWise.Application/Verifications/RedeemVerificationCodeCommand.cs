using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Verifications;

public sealed record RedeemVerificationCodeCommand(Guid SchoolId, string Code)
    : IRequest<Result<bool>>;

public sealed class RedeemVerificationCodeCommandValidator : AbstractValidator<RedeemVerificationCodeCommand>
{
    public RedeemVerificationCodeCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.Code).NotEmpty().MaximumLength(100);
    }
}

public sealed class RedeemVerificationCodeCommandHandler(
    ICurrentUser currentUser, IUserRepository users, ISchoolRepository schools, IVerificationRepository verifications)
    : IRequestHandler<RedeemVerificationCodeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RedeemVerificationCodeCommand request, CancellationToken cancellationToken)
    {
        var user = currentUser.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is not { IsActive: true, Role: UserRole.Parent })
        {
            return Result<bool>.Failure("forbidden", "An active Parent account is required.");
        }

        var now = DateTimeOffset.UtcNow;
        var code = await verifications.FindCodeAsync(request.SchoolId, VerificationCodeSecret.Hash(request.Code), cancellationToken);
        if (code is null || !code.CanRedeem(now)
            || await schools.GetByIdAsync(request.SchoolId, false, cancellationToken) is null)
        {
            return InvalidCode();
        }
        code.Redeem(user.Id, now);
        return await verifications.SaveRedemptionAsync(code, user.Id, cancellationToken)
            ? Result<bool>.Success(true)
            : InvalidCode();
    }

    private static Result<bool> InvalidCode() =>
        Result<bool>.Failure("invalid_verification_code", "The code is invalid or unavailable for this school.");
}
