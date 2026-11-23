using Microsoft.EntityFrameworkCore;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(SchoolWiseDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken) =>
        context.RefreshTokens.AddAsync(token, cancellationToken).AsTask();
}
