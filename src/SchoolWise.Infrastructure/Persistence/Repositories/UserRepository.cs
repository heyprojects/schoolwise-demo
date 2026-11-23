using Microsoft.EntityFrameworkCore;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(SchoolWiseDbContext context) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken) =>
        context.Users.AddAsync(user, cancellationToken).AsTask();
}
