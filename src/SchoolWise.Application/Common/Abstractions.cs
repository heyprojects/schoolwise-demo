using SchoolWise.Application.Schools;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Application.Common;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}

public interface ISchoolRepository
{
    Task<PagedResult<School>> SearchAsync(SchoolSearchCriteria criteria, bool includeUnapproved, CancellationToken cancellationToken);
    Task<School?> GetByIdAsync(Guid id, bool includeUnapproved, CancellationToken cancellationToken);
    Task<School?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task AddAsync(School school, CancellationToken cancellationToken);
    void Remove(School school);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string password);
}

public interface ITokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
    DateTimeOffset AccessTokenExpiresAt { get; }
    DateTimeOffset RefreshTokenExpiresAt { get; }
}

public interface ICurrentUser
{
    Guid? UserId { get; }
}
