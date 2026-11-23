using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Schools;

public sealed record SearchSchoolsQuery(
    int Page = 1,
    int PageSize = 12,
    string? SortBy = "name",
    string? SortDirection = "asc",
    string? Keyword = null,
    string? City = null,
    SchoolType? Type = null,
    bool? IsPrivate = null,
    decimal? MinTuition = null,
    decimal? MaxTuition = null,
    bool IncludeUnapproved = false) : IRequest<Result<PagedResult<SchoolListItemDto>>>;

public sealed class SearchSchoolsQueryValidator : AbstractValidator<SearchSchoolsQuery>
{
    public SearchSchoolsQueryValidator()
    {
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.SortBy).Must(value => value is null or "name" or "city" or "tuitionFrom" or "createdAt")
            .WithMessage("sortBy must be name, city, tuitionFrom, or createdAt.");
        RuleFor(query => query.SortDirection).Must(value => value is null or "asc" or "desc")
            .WithMessage("sortDirection must be asc or desc.");
        RuleFor(query => query.Keyword).MaximumLength(120);
        RuleFor(query => query.City).MaximumLength(120);
        RuleFor(query => query.MinTuition).GreaterThanOrEqualTo(0).When(query => query.MinTuition.HasValue);
        RuleFor(query => query.MaxTuition).GreaterThanOrEqualTo(0).When(query => query.MaxTuition.HasValue);
        RuleFor(query => query.Type).IsInEnum().When(query => query.Type.HasValue);
        RuleFor(query => query).Must(query => !query.MinTuition.HasValue || !query.MaxTuition.HasValue || query.MinTuition <= query.MaxTuition)
            .WithMessage("minTuition must not exceed maxTuition.");
    }
}

public sealed class SearchSchoolsQueryHandler(
    ISchoolRepository schools,
    AutoMapper.IMapper mapper) : IRequestHandler<SearchSchoolsQuery, Result<PagedResult<SchoolListItemDto>>>
{
    public async Task<Result<PagedResult<SchoolListItemDto>>> Handle(SearchSchoolsQuery request, CancellationToken cancellationToken)
    {
        var criteria = new SchoolSearchCriteria(
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDirection,
            request.Keyword?.Trim(),
            request.City?.Trim(),
            request.Type,
            request.IsPrivate,
            request.MinTuition,
            request.MaxTuition);
        var result = await schools.SearchAsync(criteria, request.IncludeUnapproved, cancellationToken);
        return Result<PagedResult<SchoolListItemDto>>.Success(
            new PagedResult<SchoolListItemDto>(mapper.Map<IReadOnlyList<SchoolListItemDto>>(result.Items), result.Page, result.PageSize, result.TotalCount));
    }
}
