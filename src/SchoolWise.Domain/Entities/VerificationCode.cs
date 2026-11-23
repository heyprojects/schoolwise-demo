namespace SchoolWise.Domain.Entities;

public sealed class VerificationCode
{
    private VerificationCode() { }

    public VerificationCode(Guid schoolId, Guid issuedBy, string secretHash,
        DateTimeOffset expiresAt, DateTimeOffset validUntil)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        IssuedBy = issuedBy;
        SecretHash = secretHash;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        ValidUntil = validUntil;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid IssuedBy { get; private set; }
    public string SecretHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset ValidUntil { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }
    public Guid? RedeemedBy { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public string? RevocationReason { get; private set; }

    public bool CanRedeem(DateTimeOffset now) =>
        RedeemedAt is null && RevokedAt is null && now < ExpiresAt && now < ValidUntil;

    public void Redeem(Guid userId, DateTimeOffset now)
    {
        if (!CanRedeem(now))
        {
            throw new InvalidOperationException("The verification code cannot be redeemed.");
        }
        RedeemedAt = now;
        RedeemedBy = userId;
    }

    public void Revoke(Guid userId, string reason, DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return;
        }
        RevokedAt = now;
        RevokedBy = userId;
        RevocationReason = reason;
    }
}
