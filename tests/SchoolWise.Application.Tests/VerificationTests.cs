using SchoolWise.Application.Verifications;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Application.Tests;

public sealed class VerificationTests
{
    [Fact]
    public void CodesAreSingleUseAndGrantsRequireTheRedeemingParent()
    {
        var now = DateTimeOffset.UtcNow;
        var code = NewCode(now);
        var parent = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => new ParentSchoolVerification(parent, code));

        code.Redeem(parent, now);
        Assert.False(code.CanRedeem(now));
        Assert.Throws<InvalidOperationException>(() => code.Redeem(Guid.NewGuid(), now));
        Assert.Throws<InvalidOperationException>(() => new ParentSchoolVerification(Guid.NewGuid(), code));
        var verification = new ParentSchoolVerification(parent, code);
        Assert.Equal(code.ValidUntil, verification.ValidUntil);
        Assert.Equal(parent, verification.UserId);
    }

    [Fact]
    public void RevokedAndExpiredCodesCannotBeRedeemed()
    {
        var now = DateTimeOffset.UtcNow;
        var code = NewCode(now);
        Assert.False(code.CanRedeem(code.ExpiresAt));
        code.Revoke(Guid.NewGuid(), "Lost code", now);
        Assert.Throws<InvalidOperationException>(() => code.Redeem(Guid.NewGuid(), now));
    }

    [Fact]
    public void HashingAllowsCopyPasteWhitespaceAndLetterCase()
    {
        var secret = VerificationCodeSecret.Create();
        Assert.Equal(48, secret.Length);
        Assert.Equal(VerificationCodeSecret.Hash(secret), VerificationCodeSecret.Hash($" {secret.ToLowerInvariant()}\n"));
        Assert.NotEqual(VerificationCodeSecret.Hash(secret), VerificationCodeSecret.Hash(VerificationCodeSecret.Create()));
    }

    [Fact]
    public void IssuanceRejectsOversizedBatchesAndInconsistentValidity()
    {
        var now = DateTimeOffset.UtcNow;
        var validator = new IssueVerificationCodesCommandValidator();
        Assert.True(validator.Validate(new IssueVerificationCodesCommand(new(Guid.NewGuid(), 5, now.AddDays(7), now.AddMonths(6)))).IsValid);
        Assert.False(validator.Validate(new IssueVerificationCodesCommand(new(Guid.NewGuid(), 51, now.AddDays(7), now.AddMonths(6)))).IsValid);
        Assert.False(validator.Validate(new IssueVerificationCodesCommand(new(Guid.NewGuid(), 1, now.AddDays(31), now.AddMonths(6)))).IsValid);
        Assert.False(validator.Validate(new IssueVerificationCodesCommand(new(Guid.NewGuid(), 1, now.AddDays(7), now.AddDays(1)))).IsValid);
        Assert.False(validator.Validate(new IssueVerificationCodesCommand(new(Guid.NewGuid(), 1, now.AddDays(7), now.AddMonths(13)))).IsValid);
    }

    private static VerificationCode NewCode(DateTimeOffset now) =>
        new(Guid.NewGuid(), Guid.NewGuid(), VerificationCodeSecret.Hash(VerificationCodeSecret.Create()),
            now.AddDays(7), now.AddMonths(6));
}
