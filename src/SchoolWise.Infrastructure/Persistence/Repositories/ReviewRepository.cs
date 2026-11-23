using Microsoft.EntityFrameworkCore;
using SchoolWise.Application.Common;
using SchoolWise.Application.Reviews;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Infrastructure.Persistence.Repositories;

public sealed class ReviewRepository(SchoolWiseDbContext context) : IReviewRepository
{
    public Task<Review?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Reviews.SingleOrDefaultAsync(review => review.Id == id, cancellationToken);

    public Task<Review?> GetForParentSchoolAsync(Guid parentId, Guid schoolId, CancellationToken cancellationToken) =>
        context.Reviews.SingleOrDefaultAsync(review => review.UserId == parentId && review.SchoolId == schoolId, cancellationToken);

    public Task<ReviewVersion> GetVersionAsync(Guid reviewId, int version, CancellationToken cancellationToken) =>
        context.ReviewVersions.SingleAsync(item => item.ReviewId == reviewId && item.Version == version, cancellationToken);

    public async Task AddAsync(Review review, CancellationToken cancellationToken) =>
        await context.Reviews.AddAsync(review, cancellationToken);

    public async Task AddVersionAsync(ReviewVersion version, CancellationToken cancellationToken) =>
        await context.ReviewVersions.AddAsync(version, cancellationToken);

    private IQueryable<Review> Published(Guid schoolId) => context.Reviews.AsNoTracking()
        .Where(review => review.SchoolId == schoolId && review.School.IsApproved
            && review.DeletedAt == null && review.Status == ReviewStatus.Published);

    public async Task<PagedResult<PublicReviewDto>> PublicAsync(Guid schoolId, int page, int pageSize, int? rating,
        string sort, CancellationToken cancellationToken)
    {
        var query = Published(schoolId);
        if (rating.HasValue) query = query.Where(review => review.Content.OverallRating == rating.Value);
        var count = await query.CountAsync(cancellationToken);
        var ordered = sort switch
        {
            "highest" => query.OrderByDescending(review => review.Content.OverallRating).ThenByDescending(review => review.UpdatedAt),
            "lowest" => query.OrderBy(review => review.Content.OverallRating).ThenByDescending(review => review.UpdatedAt),
            _ => query.OrderByDescending(review => review.UpdatedAt)
        };
        var items = await ordered.ThenBy(review => review.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(review => new PublicReviewDto(review.Id, review.Content.ToDto(), review.UpdatedAt)).ToListAsync(cancellationToken);
        return new(items, page, pageSize, count);
    }

    public async Task<SchoolRatingsDto> RatingsAsync(Guid schoolId, CancellationToken cancellationToken)
    {
        var query = Published(schoolId);
        var stats = await query.GroupBy(_ => 1).Select(group => new
        {
            Count = group.Count(),
            Overall = group.Average(review => (double)review.Content.OverallRating),
            Teaching = group.Average(review => (double)review.Content.TeachingRating),
            Safety = group.Average(review => (double)review.Content.SafetyRating),
            Communication = group.Average(review => (double)review.Content.CommunicationRating),
            Facilities = group.Average(review => (double)review.Content.FacilitiesRating),
            Activities = group.Average(review => (double)review.Content.ActivitiesRating),
            One = group.Count(review => review.Content.OverallRating == 1),
            Two = group.Count(review => review.Content.OverallRating == 2),
            Three = group.Count(review => review.Content.OverallRating == 3),
            Four = group.Count(review => review.Content.OverallRating == 4),
            Five = group.Count(review => review.Content.OverallRating == 5)
        }).SingleOrDefaultAsync(cancellationToken);
        var counts = new[] { stats?.One ?? 0, stats?.Two ?? 0, stats?.Three ?? 0, stats?.Four ?? 0, stats?.Five ?? 0 };
        return new(stats?.Count ?? 0, stats?.Overall, stats?.Teaching, stats?.Safety, stats?.Communication,
            stats?.Facilities, stats?.Activities, Enumerable.Range(1, 5)
                .Select(rating => new RatingBucket(rating, counts[rating - 1])).ToArray());
    }

    public async Task<PagedResult<ManagedReviewDto>> ManagedAsync(Guid? parentId, ReviewStatus? status, Guid? schoolId,
        int page, CancellationToken cancellationToken)
    {
        var query = context.Reviews.AsNoTracking().Where(review => review.DeletedAt == null);
        if (parentId.HasValue) query = query.Where(review => review.UserId == parentId.Value);
        else query = query.Where(review => review.Status != ReviewStatus.Draft);
        if (status.HasValue) query = query.Where(review => review.Status == status.Value);
        if (schoolId.HasValue) query = query.Where(review => review.SchoolId == schoolId.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(review => review.UpdatedAt).ThenBy(review => review.Id)
            .Skip((page - 1) * 20).Take(20)
            .Select(review => new ManagedReviewDto(review.Id, review.SchoolId, review.School.Name, review.School.Slug,
                review.Version, review.Status, review.Content.ToDto(), review.UpdatedAt,
                context.ReviewVersions.Where(version => version.ReviewId == review.Id && version.Version == review.Version)
                    .Select(version => version.ModerationNotes).SingleOrDefault())).ToListAsync(cancellationToken);
        return new(items, page, 20, count);
    }

    public async Task<PagedResult<ReviewHistoryDto>> HistoryAsync(Guid reviewId, int page, bool includeDrafts, CancellationToken cancellationToken)
    {
        var query = context.ReviewVersions.AsNoTracking().Where(version => version.ReviewId == reviewId);
        if (!includeDrafts) query = query.Where(version => version.Status != ReviewStatus.Draft);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(version => version.Version).Skip((page - 1) * 20).Take(20)
            .Select(version => new ReviewHistoryDto(version.Version, version.Status, version.Content.ToDto(),
                version.SubmittedAt, version.ReviewedAt, version.ModerationNotes, version.WithdrawnAt)).ToListAsync(cancellationToken);
        return new(items, page, 20, count);
    }
}
