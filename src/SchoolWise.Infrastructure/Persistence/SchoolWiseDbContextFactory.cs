using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolWise.Infrastructure.Persistence;

public sealed class SchoolWiseDbContextFactory : IDesignTimeDbContextFactory<SchoolWiseDbContext>
{
    public SchoolWiseDbContext CreateDbContext(string[] args)
    {
        var connectionString = FirstConfigured(
                Environment.GetEnvironmentVariable("MIGRATION_DATABASE_URL"),
                Environment.GetEnvironmentVariable("DATABASE_URL"),
                Environment.GetEnvironmentVariable("ConnectionStrings__Default"))
            ?? "Host=localhost;Database=schoolwise_design";
        var options = new DbContextOptionsBuilder<SchoolWiseDbContext>()
            .UseNpgsql(DatabaseConnectionStringResolver.Normalize(connectionString))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new SchoolWiseDbContext(options);
    }

    private static string? FirstConfigured(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
