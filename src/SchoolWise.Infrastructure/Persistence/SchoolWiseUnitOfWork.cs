using SchoolWise.Application.Common;

namespace SchoolWise.Infrastructure.Persistence;

public sealed class SchoolWiseUnitOfWork(SchoolWiseDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
