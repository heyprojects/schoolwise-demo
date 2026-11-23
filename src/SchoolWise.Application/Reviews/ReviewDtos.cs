using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Reviews;

public sealed record ReviewWriteDto(int OverallRating, int TeachingRating, int SafetyRating, int CommunicationRating,
    int FacilitiesRating, int ActivitiesRating, string? Pros, string? Cons, string ReviewText)
{
    public ReviewContent ToContent() => new(OverallRating, TeachingRating, SafetyRating, CommunicationRating,
        FacilitiesRating, ActivitiesRating, Pros, Cons, ReviewText);
}

public sealed record PublicReviewDto(Guid Id, ReviewWriteDto Content, DateTimeOffset PublishedAt);
public sealed record ManagedReviewDto(Guid Id, Guid SchoolId, string SchoolName, string SchoolSlug,
    int Version, ReviewStatus Status, ReviewWriteDto Content, DateTimeOffset UpdatedAt, string? ModerationNotes);
public sealed record ReviewHistoryDto(int Version, ReviewStatus Status, ReviewWriteDto Content,
    DateTimeOffset SubmittedAt, DateTimeOffset? ReviewedAt, string? ModerationNotes, DateTimeOffset? WithdrawnAt);
public sealed record RatingBucket(int Rating, int Count);
public sealed record SchoolRatingsDto(int ReviewCount, double? OverallScore, double? TeachingScore,
    double? SafetyScore, double? CommunicationScore, double? FacilitiesScore, double? ActivitiesScore,
    IReadOnlyList<RatingBucket> Distribution);

public static class ReviewMapping
{
    public static ReviewWriteDto ToDto(this ReviewContent content) => new(content.OverallRating, content.TeachingRating,
        content.SafetyRating, content.CommunicationRating, content.FacilitiesRating, content.ActivitiesRating,
        content.Pros, content.Cons, content.ReviewText);

    public static ManagedReviewDto ToDto(this Review review, string schoolName, string schoolSlug, string? notes) =>
        new(review.Id, review.SchoolId, schoolName, schoolSlug, review.Version, review.Status,
            review.Content.ToDto(), review.UpdatedAt, notes);
}
