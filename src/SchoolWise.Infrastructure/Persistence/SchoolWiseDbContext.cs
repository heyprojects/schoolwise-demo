using Microsoft.EntityFrameworkCore;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence;

public sealed class SchoolWiseDbContext(DbContextOptions<SchoolWiseDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<SchoolPhoto> SchoolPhotos => Set<SchoolPhoto>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchoolWiseDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
