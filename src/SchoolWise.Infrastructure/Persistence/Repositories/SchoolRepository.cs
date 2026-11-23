using Microsoft.EntityFrameworkCore;
using SchoolWise.Application.Common;
using SchoolWise.Application.Schools;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Repositories;

public sealed class SchoolRepository(SchoolWiseDbContext context) : ISchoolRepository
{
    public async Task<PagedResult<School>> SearchAsync(
        SchoolSearchCriteria criteria,
        bool includeUnapproved,
        CancellationToken cancellationToken)
    {
        IQueryable<School> query = context.Schools.AsNoTracking();
        if (!includeUnapproved)
        {
            query = query.Where(school => school.IsApproved);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            var keyword = EscapeLike(criteria.Keyword.Trim());
            var pattern = $"%{keyword}%";
            query = query.Where(school =>
                EF.Functions.ILike(school.Name, pattern, "\\") ||
                EF.Functions.ILike(school.City, pattern, "\\") ||
                EF.Functions.ILike(school.Address, pattern, "\\") ||
                (school.Description != null && EF.Functions.ILike(school.Description, pattern, "\\")));
        }

        if (!string.IsNullOrWhiteSpace(criteria.City))
        {
            query = query.Where(school => EF.Functions.ILike(school.City, EscapeLike(criteria.City.Trim()), "\\"));
        }

        if (criteria.Type.HasValue)
        {
            query = query.Where(school => school.SchoolType == criteria.Type.Value);
        }

        if (criteria.IsPrivate.HasValue)
        {
            query = query.Where(school => school.IsPrivate == criteria.IsPrivate.Value);
        }

        if (criteria.MinTuition.HasValue)
        {
            query = query.Where(school => school.TuitionTo >= criteria.MinTuition.Value);
        }

        if (criteria.MaxTuition.HasValue)
        {
            query = query.Where(school => school.TuitionFrom <= criteria.MaxTuition.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var descending = string.Equals(criteria.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (criteria.SortBy?.ToLowerInvariant(), descending) switch
        {
            ("city", false) => query.OrderBy(school => school.City).ThenBy(school => school.Name),
            ("city", true) => query.OrderByDescending(school => school.City).ThenBy(school => school.Name),
            ("tuitionfrom", false) => query.OrderBy(school => school.TuitionFrom).ThenBy(school => school.Name),
            ("tuitionfrom", true) => query.OrderByDescending(school => school.TuitionFrom).ThenBy(school => school.Name),
            ("createdat", false) => query.OrderBy(school => school.CreatedAt),
            ("createdat", true) => query.OrderByDescending(school => school.CreatedAt),
            (_, true) => query.OrderByDescending(school => school.Name),
            _ => query.OrderBy(school => school.Name)
        };

        var items = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<School>(items, criteria.Page, criteria.PageSize, totalCount);
    }

    public Task<School?> GetByIdAsync(Guid id, bool includeUnapproved, CancellationToken cancellationToken)
    {
        var query = context.Schools.Include(school => school.Photos).AsQueryable();
        if (!includeUnapproved)
        {
            query = query.Where(school => school.IsApproved);
        }

        return query.SingleOrDefaultAsync(school => school.Id == id, cancellationToken);
    }

    public Task<School?> GetBySlugAsync(string slug, CancellationToken cancellationToken) =>
        context.Schools
            .Include(school => school.Photos)
            .SingleOrDefaultAsync(school => school.Slug == slug && school.IsApproved, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken) =>
        context.Schools.AnyAsync(school => school.Slug == slug && (!excludingId.HasValue || school.Id != excludingId.Value), cancellationToken);

    public Task AddAsync(School school, CancellationToken cancellationToken) =>
        context.Schools.AddAsync(school, cancellationToken).AsTask();

    public void Remove(School school) => context.Schools.Remove(school);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
