using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Reviews;

public sealed record PublicSchoolReviewsQuery(Guid SchoolId, int Page = 1, int PageSize = 10,
    int? Rating = null, string Sort = "newest") : IRequest<Result<PagedResult<PublicReviewDto>>>;
public sealed record SchoolRatingsQuery(Guid SchoolId) : IRequest<Result<SchoolRatingsDto>>;
public sealed record ListManagedReviewsQuery(bool Admin, ReviewStatus? Status = null, Guid? SchoolId = null, int Page = 1)
    : IRequest<Result<PagedResult<ManagedReviewDto>>>;
public sealed record ReviewHistoryQuery(Guid Id, int Page = 1) : IRequest<Result<PagedResult<ReviewHistoryDto>>>;

public sealed class PublicSchoolReviewsValidator : AbstractValidator<PublicSchoolReviewsQuery>
{
    public PublicSchoolReviewsValidator()
    {
        RuleFor(query => query.SchoolId).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 100000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
        RuleFor(query => query.Rating).InclusiveBetween(1, 5).When(query => query.Rating.HasValue);
        RuleFor(query => query.Sort).Must(sort => sort is "newest" or "highest" or "lowest")
            .WithMessage("Sort must be newest, highest or lowest.");
    }
}

public sealed class SchoolRatingsValidator : AbstractValidator<SchoolRatingsQuery>
{
    public SchoolRatingsValidator() => RuleFor(query => query.SchoolId).NotEmpty();
}

public sealed class ListManagedReviewsValidator : AbstractValidator<ListManagedReviewsQuery>
{
    public ListManagedReviewsValidator()
    {
        RuleFor(query => query.Page).InclusiveBetween(1, 100000);
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
        RuleFor(query => query.SchoolId).NotEmpty().When(query => query.SchoolId.HasValue);
    }
}

public sealed class ReviewHistoryValidator : AbstractValidator<ReviewHistoryQuery>
{
    public ReviewHistoryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 100000);
    }
}

public sealed class PublicSchoolReviewsHandler(ISchoolRepository schools, IReviewRepository reviews)
    : IRequestHandler<PublicSchoolReviewsQuery, Result<PagedResult<PublicReviewDto>>>
{
    public async Task<Result<PagedResult<PublicReviewDto>>> Handle(PublicSchoolReviewsQuery request, CancellationToken cancellationToken)
    {
        if (await schools.GetByIdAsync(request.SchoolId, false, cancellationToken) is null)
            return Result<PagedResult<PublicReviewDto>>.Failure("school_not_found", "School not found.");
        return Result<PagedResult<PublicReviewDto>>.Success(await reviews.PublicAsync(request.SchoolId,
            request.Page, request.PageSize, request.Rating, request.Sort, cancellationToken));
    }
}

public sealed class SchoolRatingsHandler(ISchoolRepository schools, IReviewRepository reviews)
    : IRequestHandler<SchoolRatingsQuery, Result<SchoolRatingsDto>>
{
    public async Task<Result<SchoolRatingsDto>> Handle(SchoolRatingsQuery request, CancellationToken cancellationToken)
    {
        if (await schools.GetByIdAsync(request.SchoolId, false, cancellationToken) is null)
            return Result<SchoolRatingsDto>.Failure("school_not_found", "School not found.");
        return Result<SchoolRatingsDto>.Success(await reviews.RatingsAsync(request.SchoolId, cancellationToken));
    }
}

public sealed class ListManagedReviewsHandler(ICurrentUser actor, IUserRepository users, IReviewRepository reviews)
    : IRequestHandler<ListManagedReviewsQuery, Result<PagedResult<ManagedReviewDto>>>
{
    public async Task<Result<PagedResult<ManagedReviewDto>>> Handle(ListManagedReviewsQuery request, CancellationToken cancellationToken)
    {
        var user = actor.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is null || !user.IsActive || user.Role != (request.Admin ? UserRole.Admin : UserRole.Parent))
            return Result<PagedResult<ManagedReviewDto>>.Failure("forbidden", "Your account cannot access these reviews.");
        return Result<PagedResult<ManagedReviewDto>>.Success(await reviews.ManagedAsync(request.Admin ? null : user.Id,
            request.Status, request.SchoolId, request.Page, cancellationToken));
    }
}

public sealed class ReviewHistoryHandler(ICurrentUser actor, IUserRepository users, IReviewRepository reviews)
    : IRequestHandler<ReviewHistoryQuery, Result<PagedResult<ReviewHistoryDto>>>
{
    public async Task<Result<PagedResult<ReviewHistoryDto>>> Handle(ReviewHistoryQuery request, CancellationToken cancellationToken)
    {
        var user = actor.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is null || !user.IsActive)
            return Result<PagedResult<ReviewHistoryDto>>.Failure("forbidden", "An active account is required.");
        var review = await reviews.GetAsync(request.Id, cancellationToken);
        if (review is null || (user.Role != UserRole.Admin && review.UserId != user.Id))
            return Result<PagedResult<ReviewHistoryDto>>.Failure("review_not_found", "Review not found.");
        return Result<PagedResult<ReviewHistoryDto>>.Success(await reviews.HistoryAsync(review.Id, request.Page, cancellationToken));
    }
}
