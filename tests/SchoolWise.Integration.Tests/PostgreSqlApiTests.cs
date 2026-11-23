using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;
using SchoolWise.Infrastructure.Persistence;
using SchoolWise.Infrastructure.Security;
using Testcontainers.PostgreSql;

namespace SchoolWise.Integration.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;
    public IServiceProvider Services => _factory!.Services;

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("RUN_POSTGRES_INTEGRATION_TESTS") != "1")
        {
            return;
        }

        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _postgres.StartAsync();
            connectionString = _postgres.GetConnectionString();
        }
        _factory = new TestApiFactory(connectionString);

        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
        await database.Database.MigrateAsync();
        Client = _factory.CreateClient();
    }

    internal sealed class TestApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["DATABASE_URL"] = connectionString,
                ["Jwt:Secret"] = "integration-test-secret-with-at-least-32-bytes",
                ["Jwt:Issuer"] = "SchoolWise",
                ["Jwt:Audience"] = "SchoolWise.Web",
                ["Frontend:Origins"] = "http://localhost:3000"
            }));
            return base.CreateHost(builder);
        }
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresPostgreSqlAttribute : FactAttribute
{
    public RequiresPostgreSqlAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_POSTGRES_INTEGRATION_TESTS") != "1")
        {
            Skip = "Set RUN_POSTGRES_INTEGRATION_TESTS=1; use Docker or TEST_DATABASE_URL pointing to an isolated test database.";
        }
    }
}

public sealed class PostgreSqlApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [RequiresPostgreSql]
    public async Task HealthAndReadinessEndpointsReportStatus()
    {
        var health = await fixture.Client.GetAsync("/health");
        var readiness = await fixture.Client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
    }

    [RequiresPostgreSql]
    public async Task RegistrationAndDuplicateEmailReturnCreatedThenConflict()
    {
        var email = $"parent-{Guid.NewGuid():N}@example.gr";
        var registration = new
        {
            firstName = "Maria",
            lastName = "Papadopoulou",
            email,
            password = "Schoolwise123"
        };

        var created = await fixture.Client.PostAsJsonAsync("/api/v1/auth/register", registration);
        var duplicate = await fixture.Client.PostAsJsonAsync("/api/v1/auth/register", registration);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [RequiresPostgreSql]
    public async Task PublicSlugHidesDraftWhileAdminCanLoadIt()
    {
        var slug = $"draft-school-{Guid.NewGuid():N}";
        var school = new School("Draft school", slug, SchoolType.Kindergarten, "1 Main Street", "Athens", true);
        var approvedSlug = $"approved-school-{Guid.NewGuid():N}";
        var approvedSchool = new School("Approved school", approvedSlug, SchoolType.Elementary, "2 Main Street", "Athens", false);
        approvedSchool.Update("Approved school", approvedSlug, null, SchoolType.Elementary, "2 Main Street", "Athens",
            null, null, null, null, null, null, null, null, false, null, true);
        var adminEmail = $"admin-{Guid.NewGuid():N}@example.gr";
        const string adminPassword = "SchoolwiseAdmin123";

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
            var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            var admin = new User("Test", "Admin", adminEmail, string.Empty, UserRole.Admin);
            admin.SetPasswordHash(passwords.Hash(admin, adminPassword));
            database.Users.Add(admin);
            database.Schools.AddRange(school, approvedSchool);
            await database.SaveChangesAsync();
        }

        var approvedResponse = await fixture.Client.GetAsync($"/api/v1/schools/slug/{approvedSlug}");
        Assert.Equal(HttpStatusCode.OK, approvedResponse.StatusCode);

        var publicResponse = await fixture.Client.GetAsync($"/api/v1/schools/slug/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, publicResponse.StatusCode);

        var unauthorized = await fixture.Client.GetAsync($"/api/v1/schools/admin/{school.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        var login = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login", new { email = adminEmail, password = adminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginBody = await JsonDocument.ParseAsync(await login.Content.ReadAsStreamAsync());
        var accessToken = loginBody.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        using var adminRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/schools/admin/{school.Id}");
        adminRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var adminResponse = await fixture.Client.SendAsync(adminRequest);

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        using var adminBody = await JsonDocument.ParseAsync(await adminResponse.Content.ReadAsStreamAsync());
        Assert.False(adminBody.RootElement.GetProperty("isApproved").GetBoolean());
    }
}
