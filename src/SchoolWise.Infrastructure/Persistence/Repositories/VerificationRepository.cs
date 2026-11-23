using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolWise.Application.Common;
using SchoolWise.Application.Verifications;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Repositories;

public sealed class VerificationRepository(SchoolWiseDbContext context) : IVerificationRepository
{
    private const int PageSize = 50;

    public async Task AddCodesAsync(IEnumerable<VerificationCode> codes, CancellationToken cancellationToken) =>
        await context.VerificationCodes.AddRangeAsync(codes, cancellationToken);

    public Task<VerificationCode?> FindCodeAsync(Guid schoolId, string secretHash, CancellationToken cancellationToken) =>
        context.VerificationCodes.SingleOrDefaultAsync(
            code => code.SchoolId == schoolId && code.SecretHash == secretHash, cancellationToken);

    public Task<VerificationCode?> GetCodeAsync(Guid id, CancellationToken cancellationToken) =>
        context.VerificationCodes.SingleOrDefaultAsync(code => code.Id == id, cancellationToken);

    public async Task<bool> SaveRedemptionAsync(VerificationCode code, Guid userId, CancellationToken cancellationToken)
    {
        var existing = await context.ParentSchoolVerifications.Include(verification => verification.Code)
            .SingleOrDefaultAsync(verification => verification.UserId == userId && verification.SchoolId == code.SchoolId,
                cancellationToken);
        if (existing is null)
        {
            context.ParentSchoolVerifications.Add(new ParentSchoolVerification(userId, code));
        }
        else
        {
            if (existing.ValidUntil > code.RedeemedAt && existing.Code.RevokedAt is null)
            {
                context.ChangeTracker.Clear();
                return false;
            }
            existing.Renew(code);
        }
        return await SaveWithConcurrencyCheckAsync(cancellationToken);
    }

    public Task<bool> SaveRevocationAsync(CancellationToken cancellationToken) =>
        SaveWithConcurrencyCheckAsync(cancellationToken);

    private async Task<bool> SaveWithConcurrencyCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // SaveChanges rolls back both the claim and its grant when another request wins.
            context.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: "23505", ConstraintName: "ix_parent_school_verifications_code_id" or "ix_parent_school_verifications_user_id_school_id" })
        {
            context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<PagedResult<CodeStatusDto>> ListCodesAsync(Guid schoolId, int page, CancellationToken cancellationToken)
    {
        var query = context.VerificationCodes.AsNoTracking().Where(code => code.SchoolId == schoolId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(code => code.CreatedAt).ThenBy(code => code.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(code => new CodeStatusDto(code.Id, code.CreatedAt, code.ExpiresAt, code.ValidUntil,
                code.RedeemedAt, code.RevokedAt)).ToListAsync(cancellationToken);
        return new PagedResult<CodeStatusDto>(items, page, PageSize, count);
    }

    public async Task<PagedResult<ParentVerificationDto>> ListOwnAsync(Guid userId, int page, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var query = context.ParentSchoolVerifications.AsNoTracking().Where(verification => verification.UserId == userId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(verification => verification.VerifiedAt).ThenBy(verification => verification.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(verification => new ParentVerificationDto(verification.Id, verification.SchoolId, verification.School.Name,
                verification.VerifiedAt, verification.ValidUntil,
                verification.Code.RevokedAt != null ? "Revoked" : verification.ValidUntil <= now ? "Expired" : "Active"))
            .ToListAsync(cancellationToken);
        return new PagedResult<ParentVerificationDto>(items, page, PageSize, count);
    }
}
