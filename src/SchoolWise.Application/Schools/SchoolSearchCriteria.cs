using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Schools;

public sealed record SchoolSearchCriteria(
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDirection,
    string? Keyword,
    string? City,
    SchoolType? Type,
    bool? IsPrivate,
    decimal? MinTuition,
    decimal? MaxTuition);
