using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Application.Verifications;

public interface IVerificationRepository
{
    Task AddCodesAsync(IEnumerable<VerificationCode> codes, CancellationToken cancellationToken);
    Task<VerificationCode?> FindCodeAsync(Guid schoolId, string secretHash, CancellationToken cancellationToken);
    Task<VerificationCode?> GetCodeAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SaveRedemptionAsync(VerificationCode code, Guid userId, CancellationToken cancellationToken);
    Task<bool> SaveRevocationAsync(CancellationToken cancellationToken);
    Task<PagedResult<CodeStatusDto>> ListCodesAsync(Guid schoolId, int page, CancellationToken cancellationToken);
    Task<PagedResult<ParentVerificationDto>> ListOwnAsync(Guid userId, int page, DateTimeOffset now,
        CancellationToken cancellationToken);
}
