using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Verifications;

public sealed record IssueVerificationCodesCommand(IssueCodesDto Input) : IRequest<Result<IReadOnlyList<IssuedCodeDto>>>;

public sealed class IssueVerificationCodesCommandValidator : AbstractValidator<IssueVerificationCodesCommand>
{
    public IssueVerificationCodesCommandValidator()
    {
        RuleFor(command => command.Input).NotNull();
        When(command => command.Input is not null, () =>
        {
            RuleFor(command => command.Input.SchoolId).NotEmpty();
            RuleFor(command => command.Input.Count).InclusiveBetween(1, 50);
            RuleFor(command => command.Input.ExpiresAt).Must(value =>
                value > DateTimeOffset.UtcNow && value <= DateTimeOffset.UtcNow.AddDays(30))
                .WithMessage("Code expiry must be in the future and within 30 days.");
            RuleFor(command => command.Input.ValidUntil).Must(value =>
                value > DateTimeOffset.UtcNow && value <= DateTimeOffset.UtcNow.AddMonths(12))
                .WithMessage("Verification validity must be in the future and within 12 months.");
            RuleFor(command => command.Input.ValidUntil).GreaterThanOrEqualTo(command => command.Input.ExpiresAt);
        });
    }
}

public sealed class IssueVerificationCodesCommandHandler(
    ICurrentUser currentUser, IUserRepository users, ISchoolRepository schools,
    IVerificationRepository verifications, IUnitOfWork unitOfWork)
    : IRequestHandler<IssueVerificationCodesCommand, Result<IReadOnlyList<IssuedCodeDto>>>
{
    public async Task<Result<IReadOnlyList<IssuedCodeDto>>> Handle(IssueVerificationCodesCommand request,
        CancellationToken cancellationToken)
    {
        var user = currentUser.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is not { IsActive: true, Role: UserRole.Admin })
        {
            return Result<IReadOnlyList<IssuedCodeDto>>.Failure("forbidden", "Administrator access is required.");
        }
        if (await schools.GetByIdAsync(request.Input.SchoolId, false, cancellationToken) is null)
        {
            return Result<IReadOnlyList<IssuedCodeDto>>.Failure("school_not_found", "Approved school not found.");
        }

        var codes = Enumerable.Range(0, request.Input.Count).Select(_ =>
        {
            var secret = VerificationCodeSecret.Create();
            var code = new VerificationCode(request.Input.SchoolId, user.Id, VerificationCodeSecret.Hash(secret),
                request.Input.ExpiresAt.ToUniversalTime(), request.Input.ValidUntil.ToUniversalTime());
            return (Entity: code, Secret: secret);
        }).ToArray();
        await verifications.AddCodesAsync(codes.Select(code => code.Entity), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<IReadOnlyList<IssuedCodeDto>>.Success(
            codes.Select(code => new IssuedCodeDto(code.Entity.Id, code.Secret)).ToArray());
    }
}
