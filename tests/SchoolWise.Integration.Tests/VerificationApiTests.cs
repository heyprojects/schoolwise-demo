using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolWise.Application.Common;
using SchoolWise.Application.Verifications;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;
using SchoolWise.Infrastructure.Persistence;

namespace SchoolWise.Integration.Tests;

public sealed class VerificationApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [RequiresPostgreSql]
    public async Task AdminIssuesParentRedeemsAndRevocationEndsEligibilityWithoutLeakingCodes()
    {
        var (school, admin, parent, other) = await SeedAsync();
        using var issue = await SendAsync(HttpMethod.Post, "/codes", admin,
            new IssueCodesDto(school.Id, 2, DateTimeOffset.UtcNow.AddDays(7), DateTimeOffset.UtcNow.AddMonths(6)));
        Assert.Equal(HttpStatusCode.OK, issue.StatusCode);
        Assert.Contains("no-store", issue.Headers.CacheControl!.ToString());
        var issuedCodes = (await issue.Content.ReadFromJsonAsync<List<IssuedCodeDto>>())!;
        Assert.Equal(2, issuedCodes.Count);
        var code = issuedCodes[0];
        var renewal = issuedCodes[1];

        using (var scope = fixture.Services.CreateScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>()
                .VerificationCodes.SingleAsync(item => item.Id == code.Id);
            Assert.Equal(VerificationCodeSecret.Hash(code.Code), stored.SecretHash);
        }
        using var listing = await SendAsync(HttpMethod.Get, $"/codes?schoolId={school.Id}", admin);
        Assert.DoesNotContain(code.Code, await listing.Content.ReadAsStringAsync());
        using var forbidden = await SendAsync(HttpMethod.Get, $"/codes?schoolId={school.Id}", parent);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var wrongSchool = await SendAsync(HttpMethod.Post, "/redeem", parent,
            new { schoolId = Guid.NewGuid(), code = code.Code });
        Assert.Equal(HttpStatusCode.BadRequest, wrongSchool.StatusCode);
        using var redeem = await SendAsync(HttpMethod.Post, "/redeem", parent,
            new { schoolId = school.Id, code = $" {code.Code.ToLowerInvariant()} " });
        Assert.Equal(HttpStatusCode.OK, redeem.StatusCode);
        using var duplicateGrant = await SendAsync(HttpMethod.Post, "/redeem", parent,
            new { schoolId = school.Id, code = renewal.Code });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateGrant.StatusCode);
        using var reuse = await SendAsync(HttpMethod.Post, "/redeem", other, new { schoolId = school.Id, code = code.Code });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        var wrongSchoolError = await wrongSchool.Content.ReadFromJsonAsync<JsonElement>();
        var reusedCodeError = await reuse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(wrongSchoolError.GetProperty("title").GetString(), reusedCodeError.GetProperty("title").GetString());

        using var own = await SendAsync(HttpMethod.Get, "/me", parent);
        Assert.Equal("Active", Assert.Single((await own.Content.ReadFromJsonAsync<PagedResult<ParentVerificationDto>>())!.Items).Status);
        using var others = await SendAsync(HttpMethod.Get, "/me", other);
        Assert.Empty((await others.Content.ReadFromJsonAsync<PagedResult<ParentVerificationDto>>())!.Items);
        using var revoke = await SendAsync(HttpMethod.Post, $"/codes/{code.Id}/revoke", admin, new { reason = "Pilot revocation test" });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        using var afterRevocation = await SendAsync(HttpMethod.Get, "/me", parent);
        Assert.Equal("Revoked", Assert.Single((await afterRevocation.Content.ReadFromJsonAsync<PagedResult<ParentVerificationDto>>())!.Items).Status);
        using var renewed = await SendAsync(HttpMethod.Post, "/redeem", parent,
            new { schoolId = school.Id, code = renewal.Code });
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        using var afterRenewal = await SendAsync(HttpMethod.Get, "/me", parent);
        Assert.Equal("Active", Assert.Single((await afterRenewal.Content.ReadFromJsonAsync<PagedResult<ParentVerificationDto>>())!.Items).Status);
    }

    [RequiresPostgreSql]
    public async Task ConcurrentRedemptionsCommitOnlyOneGrant()
    {
        var (school, admin, parent, other) = await SeedAsync();
        var rawCode = VerificationCodeSecret.Create();
        var code = new VerificationCode(school.Id, admin.Id, VerificationCodeSecret.Hash(rawCode),
            DateTimeOffset.UtcNow.AddDays(7), DateTimeOffset.UtcNow.AddMonths(6));
        using (var seed = fixture.Services.CreateScope())
        {
            var database = seed.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
            database.VerificationCodes.Add(code);
            await database.SaveChangesAsync();
        }
        using var first = fixture.Services.CreateScope();
        using var second = fixture.Services.CreateScope();
        var repo1 = first.ServiceProvider.GetRequiredService<IVerificationRepository>();
        var repo2 = second.ServiceProvider.GetRequiredService<IVerificationRepository>();
        var code1 = (await repo1.GetCodeAsync(code.Id, default))!;
        var code2 = (await repo2.GetCodeAsync(code.Id, default))!;
        code1.Redeem(parent.Id, DateTimeOffset.UtcNow);
        code2.Redeem(other.Id, DateTimeOffset.UtcNow);
        var results = await Task.WhenAll(repo1.SaveRedemptionAsync(code1, parent.Id, default),
            repo2.SaveRedemptionAsync(code2, other.Id, default));
        Assert.Single(results, result => result);
        using var check = fixture.Services.CreateScope();
        Assert.Equal(1, await check.ServiceProvider.GetRequiredService<SchoolWiseDbContext>()
            .ParentSchoolVerifications.CountAsync(verification => verification.CodeId == code.Id));
    }

    [RequiresPostgreSql]
    public async Task ExpiredRevokedAndInactiveAccountsCannotRedeemAndAttemptsAreLimited()
    {
        var (school, admin, parent, other) = await SeedAsync();
        var expiredSecret = VerificationCodeSecret.Create();
        var revokedSecret = VerificationCodeSecret.Create();
        using (var scope = fixture.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
            var expired = new VerificationCode(school.Id, admin.Id, VerificationCodeSecret.Hash(expiredSecret),
                DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.AddDays(30));
            // Create a formerly valid code without bypassing the database validity constraint.
            database.VerificationCodes.Add(expired);
            database.Entry(expired).Property(code => code.CreatedAt).CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
            var revoked = new VerificationCode(school.Id, admin.Id, VerificationCodeSecret.Hash(revokedSecret),
                DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(30));
            revoked.Revoke(admin.Id, "Test", DateTimeOffset.UtcNow);
            database.VerificationCodes.Add(revoked);
            var inactive = await database.Users.SingleAsync(user => user.Id == other.Id);
            inactive.Deactivate();
            await database.SaveChangesAsync();
        }
        using var rejectedInactive = await SendAsync(HttpMethod.Post, "/redeem", other, new { schoolId = school.Id, code = "unknown" });
        Assert.Equal(HttpStatusCode.Forbidden, rejectedInactive.StatusCode);
        foreach (var value in new[] { expiredSecret, revokedSecret, "unknown", "unknown", "unknown" })
        {
            using var invalid = await SendAsync(HttpMethod.Post, "/redeem", parent, new { schoolId = school.Id, code = value });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        using var limited = await SendAsync(HttpMethod.Post, "/redeem", parent, new { schoolId = school.Id, code = "unknown" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter);
        using var rejectedAdmin = await SendAsync(HttpMethod.Post, "/redeem", admin, new { schoolId = school.Id, code = "unknown" });
        Assert.Equal(HttpStatusCode.Forbidden, rejectedAdmin.StatusCode);
    }

    private async Task<(School School, User Admin, User Parent, User Other)> SeedAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
        var school = new School("Pilot test school", $"pilot-{Guid.NewGuid():N}", SchoolType.Elementary, "Test address", "Athens", true);
        school.Update(school.Name, school.Slug, null, school.SchoolType, school.Address, school.City, null,
            null, null, null, null, null, null, null, true, null, true);
        var admin = new User("Test", "Admin", $"{Guid.NewGuid():N}@example.invalid", "unused", UserRole.Admin);
        var parent = new User("Test", "Parent", $"{Guid.NewGuid():N}@example.invalid", "unused");
        var other = new User("Test", "Parent", $"{Guid.NewGuid():N}@example.invalid", "unused");
        database.AddRange(school, admin, parent, other);
        await database.SaveChangesAsync();
        return (school, admin, parent, other);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, User user, object? body = null)
    {
        using var scope = fixture.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().CreateAccessToken(user);
        using var request = new HttpRequestMessage(method, $"/api/v1/verifications{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return await fixture.Client.SendAsync(request);
    }
}
