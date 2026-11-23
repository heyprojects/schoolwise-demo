namespace SchoolWise.Domain.Entities;

public sealed class ParentSchoolVerification
{
    private ParentSchoolVerification() { }

    public ParentSchoolVerification(Guid userId, VerificationCode code)
    {
        if (code.RedeemedBy != userId || code.RedeemedAt is null)
        {
            throw new InvalidOperationException("Verification requires a code redeemed by this parent.");
        }
        Id = Guid.NewGuid();
        UserId = userId;
        CodeId = code.Id;
        SchoolId = code.SchoolId;
        VerifiedAt = code.RedeemedAt.Value;
        ValidUntil = code.ValidUntil;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid CodeId { get; private set; }
    public DateTimeOffset VerifiedAt { get; private set; }
    public DateTimeOffset ValidUntil { get; private set; }
    public VerificationCode Code { get; private set; } = null!;
    public School School { get; private set; } = null!;

    public void Renew(VerificationCode code)
    {
        if (code.RedeemedBy != UserId || code.RedeemedAt is null || code.SchoolId != SchoolId)
        {
            throw new InvalidOperationException("Renewal requires a code redeemed by this parent for the same school.");
        }
        CodeId = code.Id;
        VerifiedAt = code.RedeemedAt.Value;
        ValidUntil = code.ValidUntil;
    }
}
