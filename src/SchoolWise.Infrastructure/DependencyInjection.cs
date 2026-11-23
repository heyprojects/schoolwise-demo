using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchoolWise.Application.Common;
using SchoolWise.Application.Verifications;
using SchoolWise.Application.Reviews;
using SchoolWise.Infrastructure.Persistence;
using SchoolWise.Infrastructure.Persistence.Repositories;
using SchoolWise.Infrastructure.Security;

namespace SchoolWise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration.GetConnectionString("Default");
        }
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DATABASE_URL or ConnectionStrings:Default must be configured.");
        }
        services.AddDbContext<SchoolWiseDbContext>(options =>
            options.UseNpgsql(DatabaseConnectionStringResolver.Normalize(connectionString)).UseSnakeCaseNamingConvention());
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISchoolRepository, SchoolRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IVerificationRepository, VerificationRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IUnitOfWork, SchoolWiseUnitOfWork>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<DatabaseSeeder>();
        return services;
    }
}
