namespace SchoolWise.Application.Verifications;

public sealed record IssueCodesDto(Guid SchoolId, int Count, DateTimeOffset ExpiresAt, DateTimeOffset ValidUntil);
public sealed record IssuedCodeDto(Guid Id, string Code);
public sealed record CodeStatusDto(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt,
    DateTimeOffset ValidUntil, DateTimeOffset? RedeemedAt, DateTimeOffset? RevokedAt);
public sealed record ParentVerificationDto(Guid Id, Guid SchoolId, string SchoolName,
    DateTimeOffset VerifiedAt, DateTimeOffset ValidUntil, string Status);
