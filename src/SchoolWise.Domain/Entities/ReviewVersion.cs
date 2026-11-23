using SchoolWise.Domain.Enums;

namespace SchoolWise.Domain.Entities;

public sealed class ReviewVersion
{
    private ReviewVersion() { }

    public ReviewVersion(Review review)
    {
        Id = Guid.NewGuid();
        ReviewId = review.Id;
        Version = review.Version;
        Content = review.Content;
        Status = review.Status;
        SubmittedAt = review.UpdatedAt;
    }

    public Guid Id { get; private set; }
    public Guid ReviewId { get; private set; }
    public int Version { get; private set; }
    public ReviewContent Content { get; private set; } = null!;
    public ReviewStatus Status { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public string? ModerationNotes { get; private set; }
    public DateTimeOffset? WithdrawnAt { get; private set; }

    public void Moderate(Guid adminId, bool approve, string? notes)
    {
        if (Status != ReviewStatus.PendingModeration || WithdrawnAt is not null)
        {
            throw new InvalidOperationException("This version cannot be moderated.");
        }
        Status = approve ? ReviewStatus.Published : ReviewStatus.Rejected;
        ReviewedBy = adminId;
        ReviewedAt = DateTimeOffset.UtcNow;
        ModerationNotes = notes?.Trim();
    }

    public void Withdraw(DateTimeOffset now) => WithdrawnAt ??= now;
}
