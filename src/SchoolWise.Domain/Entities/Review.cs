using SchoolWise.Domain.Enums;

namespace SchoolWise.Domain.Entities;

public sealed class Review
{
    private Review() { }

    public Review(Guid schoolId, Guid userId, ReviewContent content, bool saveAsDraft = false)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        UserId = userId;
        Content = content;
        Version = 1;
        Status = saveAsDraft ? ReviewStatus.Draft : ReviewStatus.PendingModeration;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid UserId { get; private set; }
    public ReviewContent Content { get; private set; } = null!;
    public ReviewStatus Status { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public School School { get; private set; } = null!;

    public void Resubmit(ReviewContent content, bool saveAsDraft = false)
    {
        Content = content;
        Version++;
        Status = saveAsDraft ? ReviewStatus.Draft : ReviewStatus.PendingModeration;
        UpdatedAt = DateTimeOffset.UtcNow;
        DeletedAt = null;
    }

    public void Moderate(int version, bool approve)
    {
        if (DeletedAt is not null || Version != version || Status != ReviewStatus.PendingModeration)
        {
            throw new InvalidOperationException("Only the current pending review version can be moderated.");
        }
        Status = approve ? ReviewStatus.Published : ReviewStatus.Rejected;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete()
    {
        DeletedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }
}
