using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Reviews;

public interface IReviewRepository
{
    Task<Review?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Review?> GetForParentSchoolAsync(Guid parentId, Guid schoolId, CancellationToken cancellationToken);
    Task<ReviewVersion> GetVersionAsync(Guid reviewId, int version, CancellationToken cancellationToken);
    Task AddAsync(Review review, CancellationToken cancellationToken);
    Task AddVersionAsync(ReviewVersion version, CancellationToken cancellationToken);
    Task<PagedResult<PublicReviewDto>> PublicAsync(Guid schoolId, int page, int pageSize, int? rating, string sort,
        CancellationToken cancellationToken);
    Task<SchoolRatingsDto> RatingsAsync(Guid schoolId, CancellationToken cancellationToken);
    Task<PagedResult<ManagedReviewDto>> ManagedAsync(Guid? parentId, ReviewStatus? status, Guid? schoolId, int page,
        CancellationToken cancellationToken);
    Task<PagedResult<ReviewHistoryDto>> HistoryAsync(Guid reviewId, int page, CancellationToken cancellationToken);
}
