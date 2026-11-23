using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Schools;

public sealed record SchoolPhotoDto(Guid Id, string Url, int SortOrder);

public sealed record SchoolDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    SchoolType SchoolType,
    string Address,
    string City,
    string? PostalCode,
    string? Phone,
    string? Email,
    string? Website,
    decimal? TuitionFrom,
    decimal? TuitionTo,
    int? StudentCapacity,
    int? EstablishedYear,
    bool IsPrivate,
    string? LogoUrl,
    bool IsApproved,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SchoolPhotoDto> Photos);

public sealed record SchoolListItemDto(
    Guid Id,
    string Name,
    string Slug,
    SchoolType SchoolType,
    string Address,
    string City,
    decimal? TuitionFrom,
    decimal? TuitionTo,
    bool IsPrivate,
    bool IsApproved,
    string? LogoUrl);

public sealed record SchoolPhotoInput(string Url, int SortOrder);

public sealed record SchoolWriteDto(
    string Name,
    string? Description,
    SchoolType SchoolType,
    string Address,
    string City,
    string? PostalCode,
    string? Phone,
    string? Email,
    string? Website,
    decimal? TuitionFrom,
    decimal? TuitionTo,
    int? StudentCapacity,
    int? EstablishedYear,
    bool IsPrivate,
    string? LogoUrl,
    bool IsApproved,
    IReadOnlyList<SchoolPhotoInput> Photos);
