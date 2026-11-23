using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Verifications;

public sealed record RevokeVerificationCodeCommand(Guid Id, string Reason) : IRequest<Result<bool>>;

public sealed class RevokeVerificationCodeCommandValidator : AbstractValidator<RevokeVerificationCodeCommand>
{
    public RevokeVerificationCodeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class RevokeVerificationCodeCommandHandler(
    ICurrentUser currentUser, IUserRepository users, IVerificationRepository verifications)
    : IRequestHandler<RevokeVerificationCodeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RevokeVerificationCodeCommand request, CancellationToken cancellationToken)
    {
        var user = currentUser.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is not { IsActive: true, Role: UserRole.Admin })
        {
            return Result<bool>.Failure("forbidden", "Administrator access is required.");
        }
        var code = await verifications.GetCodeAsync(request.Id, cancellationToken);
        if (code is null)
        {
            return Result<bool>.Failure("verification_code_not_found", "Verification code not found.");
        }
        code.Revoke(user.Id, request.Reason.Trim(), DateTimeOffset.UtcNow);
        return await verifications.SaveRevocationAsync(cancellationToken)
            ? Result<bool>.Success(true)
            : Result<bool>.Failure("verification_conflict", "The code changed. Reload and retry.");
    }
}
