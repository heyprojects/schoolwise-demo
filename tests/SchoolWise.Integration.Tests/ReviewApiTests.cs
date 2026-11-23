using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolWise.Application.Common;
using SchoolWise.Application.Reviews;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;
using SchoolWise.Infrastructure.Persistence;

namespace SchoolWise.Integration.Tests;

public sealed class ReviewApiTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    [RequiresPostgreSql]
    public async Task RegisteredParentCanSubmitWithoutVerificationAndAdminPublishesForVisitors()
    {
        var (school, admin, _, _) = await SeedAsync();
        var email = $"reviews-{Guid.NewGuid():N}@example.invalid";
        const string password = "SyntheticParentOnly123!";
        using var registered = await fixture.Client.PostAsJsonAsync("/api/v1/auth/register",
            new { firstName = "Test", lastName = "Parent", email, password });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        using var login = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        var session = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = session.GetProperty("accessToken").GetString()!;
        using var created = await SendTokenAsync(HttpMethod.Post, "/reviews", token,
            new { schoolId = school.Id, content = Content(4) });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var review = (await created.Content.ReadFromJsonAsync<ManagedReviewDto>(JsonOptions))!;
        Assert.Equal(ReviewStatus.PendingModeration, review.Status);
        var empty = await RatingsAsync(school.Id);
        Assert.Equal(0, empty.ReviewCount);
        Assert.Null(empty.OverallScore);
        using var queue = await SendAsync(HttpMethod.Get, "/reviews/moderation", admin);
        var pending = (await queue.Content.ReadFromJsonAsync<PagedResult<ManagedReviewDto>>(JsonOptions))!;
        Assert.Contains(pending.Items, item => item.Id == review.Id);
        using var approved = await SendAsync(HttpMethod.Post, $"/reviews/{review.Id}/moderation", admin,
            new { version = 1, approve = true, notes = "Suitable for publication." });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal(4d, (await RatingsAsync(school.Id)).OverallScore);
        using var published = await fixture.Client.GetAsync($"/api/v1/schools/{school.Id}/reviews");
        var json = await published.Content.ReadAsStringAsync();
        Assert.DoesNotContain("userId", json);
        Assert.DoesNotContain("moderationNotes", json);
        Assert.DoesNotContain("Suitable for publication", json);
        Assert.Contains("no-store", published.Headers.CacheControl!.ToString());
        var list = (await published.Content.ReadFromJsonAsync<PagedResult<PublicReviewDto>>())!;
        Assert.Equal(review.Id, Assert.Single(list.Items).Id);
    }

    [RequiresPostgreSql]
    public async Task EditsUnpublishStaleDecisionsFailAndDeletionRetainsVersionHistory()
    {
        var (school, admin, parent, _) = await SeedAsync();
        var review = await CreateAsync(school.Id, parent);
        using var approved = await ModerateAsync(review.Id, 1, true, admin);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var edited = await SendAsync(HttpMethod.Put, $"/reviews/{review.Id}", parent,
            new { version = 1, content = Content(2) });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Equal(0, (await RatingsAsync(school.Id)).ReviewCount);
        using var stale = await ModerateAsync(review.Id, 1, true, admin);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var rejected = await ModerateAsync(review.Id, 2, false, admin);
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        using var own = await SendAsync(HttpMethod.Get, "/reviews/mine?status=Rejected", parent);
        var ownReview = Assert.Single((await own.Content.ReadFromJsonAsync<PagedResult<ManagedReviewDto>>(JsonOptions))!.Items);
        Assert.Equal("Please add context.", ownReview.ModerationNotes);
        using var resubmitted = await SendAsync(HttpMethod.Put, $"/reviews/{review.Id}", parent,
            new { version = 2, content = Content(5) });
        Assert.Equal(HttpStatusCode.OK, resubmitted.StatusCode);
        using var republished = await ModerateAsync(review.Id, 3, true, admin);
        Assert.Equal(HttpStatusCode.OK, republished.StatusCode);
        Assert.Equal(5d, (await RatingsAsync(school.Id)).OverallScore);
        using var deleted = await SendAsync(HttpMethod.Delete, $"/reviews/{review.Id}?version=3", parent);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(0, (await RatingsAsync(school.Id)).ReviewCount);
        using var history = await SendAsync(HttpMethod.Get, $"/reviews/{review.Id}/history", parent);
        var versions = (await history.Content.ReadFromJsonAsync<PagedResult<ReviewHistoryDto>>(JsonOptions))!;
        Assert.Equal(3, versions.TotalCount);
        Assert.Equal(new[] { 5, 2, 4 }, versions.Items.Select(item => item.Content.OverallRating));
        Assert.All(versions.Items, item => Assert.NotNull(item.WithdrawnAt));
        var recreated = await CreateAsync(school.Id, parent);
        Assert.Equal(review.Id, recreated.Id);
        Assert.Equal(4, recreated.Version);
        Assert.Equal(ReviewStatus.PendingModeration, recreated.Status);
    }

    [RequiresPostgreSql]
    public async Task OwnershipRoleValidationAndHiddenSchoolBoundariesAreEnforced()
    {
        var (school, admin, parent, other) = await SeedAsync();
        var review = await CreateAsync(school.Id, parent);
        using var edit = await SendAsync(HttpMethod.Put, $"/reviews/{review.Id}", other, new { version = 1, content = Content(1) });
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
        using var deletion = await SendAsync(HttpMethod.Delete, $"/reviews/{review.Id}?version=1", other);
        Assert.Equal(HttpStatusCode.NotFound, deletion.StatusCode);
        using var history = await SendAsync(HttpMethod.Get, $"/reviews/{review.Id}/history", other);
        Assert.Equal(HttpStatusCode.NotFound, history.StatusCode);
        using var moderation = await ModerateAsync(review.Id, 1, true, parent);
        Assert.Equal(HttpStatusCode.Forbidden, moderation.StatusCode);
        using var invalid = await SendAsync(HttpMethod.Post, "/reviews", other,
            new { schoolId = school.Id, content = Content(0) });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var adminWrite = await SendAsync(HttpMethod.Post, "/reviews", admin,
            new { schoolId = school.Id, content = Content(5) });
        Assert.Equal(HttpStatusCode.Forbidden, adminWrite.StatusCode);
        using var invalidSort = await fixture.Client.GetAsync($"/api/v1/schools/{school.Id}/reviews?sort=unsafe");
        Assert.Equal(HttpStatusCode.BadRequest, invalidSort.StatusCode);
        using var deleteSchool = await SendAsync(HttpMethod.Delete, $"/schools/{school.Id}", admin);
        Assert.Equal(HttpStatusCode.Conflict, deleteSchool.StatusCode);
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
            var tracked = await db.Schools.SingleAsync(item => item.Id == school.Id);
            tracked.Update(tracked.Name, tracked.Slug, null, tracked.SchoolType, tracked.Address, tracked.City,
                null, null, null, null, null, null, null, null, true, null, false);
            await db.SaveChangesAsync();
        }
        using var hidden = await fixture.Client.GetAsync($"/api/v1/schools/{school.Id}/reviews");
        using var hiddenRatings = await fixture.Client.GetAsync($"/api/v1/schools/{school.Id}/ratings");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, hiddenRatings.StatusCode);
    }

    [RequiresPostgreSql]
    public async Task ConcurrentDuplicateSubmissionsProduceOneReviewAndOneVersion()
    {
        var (school, _, parent, _) = await SeedAsync();
        var responses = await Task.WhenAll(
            SendAsync(HttpMethod.Post, "/reviews", parent, new { schoolId = school.Id, content = Content(4) }),
            SendAsync(HttpMethod.Post, "/reviews", parent, new { schoolId = school.Id, content = Content(3) }));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            using var scope = fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
            var review = await db.Reviews.SingleAsync(item => item.SchoolId == school.Id && item.UserId == parent.Id);
            Assert.Equal(1, await db.ReviewVersions.CountAsync(version => version.ReviewId == review.Id));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [RequiresPostgreSql]
    public async Task ConcurrentEditAndModerationCannotPublishAnUnapprovedVersion()
    {
        var (school, admin, parent, _) = await SeedAsync();
        var created = await CreateAsync(school.Id, parent);
        using var first = fixture.Services.CreateScope();
        using var second = fixture.Services.CreateScope();
        var db1 = first.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
        var db2 = second.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
        var review1 = await db1.Reviews.SingleAsync(item => item.Id == created.Id);
        var review2 = await db2.Reviews.SingleAsync(item => item.Id == created.Id);
        var version1 = await db1.ReviewVersions.SingleAsync(item => item.ReviewId == created.Id);
        var version2 = await db2.ReviewVersions.SingleAsync(item => item.ReviewId == created.Id);
        review1.Moderate(1, true);
        version1.Moderate(admin.Id, true, null);
        version2.Withdraw(DateTimeOffset.UtcNow);
        review2.Resubmit(Content(1).ToContent());
        db2.ReviewVersions.Add(new ReviewVersion(review2));
        var results = await Task.WhenAll(Record.ExceptionAsync(() => db1.SaveChangesAsync()),
            Record.ExceptionAsync(() => db2.SaveChangesAsync()));
        Assert.Single(results, exception => exception is null);
        Assert.IsType<DbUpdateConcurrencyException>(Assert.Single(results, exception => exception is not null));
        using var check = fixture.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
        var actual = await db.Reviews.SingleAsync(item => item.Id == created.Id);
        var snapshot = await db.ReviewVersions.SingleAsync(item => item.ReviewId == actual.Id && item.Version == actual.Version);
        Assert.Equal(actual.Status, snapshot.Status);
        if (actual.Status == ReviewStatus.Published)
        {
            Assert.Equal(1, actual.Version);
            Assert.Equal(4d, (await RatingsAsync(school.Id)).OverallScore);
        }
        else Assert.Equal(0, (await RatingsAsync(school.Id)).ReviewCount);
    }

    [RequiresPostgreSql]
    public async Task AveragesDistributionAndFilteredPagingIncludeOnlyPublishedNonDeletedReviews()
    {
        var (school, admin, parent, other) = await SeedAsync();
        var first = await CreateAsync(school.Id, parent, 2, Content(2) with
        {
            TeachingRating = 1, SafetyRating = 3, CommunicationRating = 4, FacilitiesRating = 5, ActivitiesRating = 1
        });
        var second = await CreateAsync(school.Id, other, 5);
        using var a = await ModerateAsync(first.Id, 1, true, admin);
        using var b = await ModerateAsync(second.Id, 1, true, admin);
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
            foreach (var state in new[] { "pending", "rejected", "deleted" })
            {
                var author = new User("Test", "Parent", $"{Guid.NewGuid():N}@example.invalid", "unused");
                var review = new Review(school.Id, author.Id, Content(1).ToContent());
                var version = new ReviewVersion(review);
                if (state != "pending")
                {
                    review.Moderate(1, state == "deleted");
                    version.Moderate(admin.Id, state == "deleted", "Synthetic test");
                }
                if (state == "deleted") { review.Delete(); version.Withdraw(DateTimeOffset.UtcNow); }
                db.AddRange(author, review, version);
            }
            await db.SaveChangesAsync();
        }
        var summary = await RatingsAsync(school.Id);
        Assert.Equal(2, summary.ReviewCount);
        Assert.Equal(3.5, summary.OverallScore);
        Assert.Equal(3d, summary.TeachingScore);
        Assert.Equal(4d, summary.SafetyScore);
        Assert.Equal(4.5, summary.CommunicationScore);
        Assert.Equal(5d, summary.FacilitiesScore);
        Assert.Equal(3d, summary.ActivitiesScore);
        Assert.Equal(2, summary.Distribution.Sum(bucket => bucket.Count));
        using var filtered = await fixture.Client.GetAsync($"/api/v1/schools/{school.Id}/reviews?rating=5&pageSize=1");
        var page = (await filtered.Content.ReadFromJsonAsync<PagedResult<PublicReviewDto>>())!;
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(second.Id, Assert.Single(page.Items).Id);
        using var sorted = await fixture.Client.GetAsync($"/api/v1/schools/{school.Id}/reviews?sort=lowest&pageSize=1&page=2");
        Assert.Equal(second.Id, Assert.Single((await sorted.Content.ReadFromJsonAsync<PagedResult<PublicReviewDto>>())!.Items).Id);
    }

    [RequiresPostgreSql]
    public async Task IncompleteDraftIsOwnerOnlyAndSubmissionRequiresCompleteContent()
    {
        var (school, admin, parent, other) = await SeedAsync();
        var incomplete = Content(0) with { Pros = null, Cons = null, ReviewText = "" };
        using var created = await SendAsync(HttpMethod.Post, "/reviews", parent,
            new { schoolId = school.Id, content = incomplete, saveAsDraft = true });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var draft = (await created.Content.ReadFromJsonAsync<ManagedReviewDto>(JsonOptions))!;
        Assert.Equal(ReviewStatus.Draft, draft.Status);
        Assert.Equal("", draft.Content.ReviewText);
        using var mine = await SendAsync(HttpMethod.Get, "/reviews/mine?status=Draft", parent);
        Assert.Equal(draft.Id, Assert.Single((await mine.Content.ReadFromJsonAsync<PagedResult<ManagedReviewDto>>(JsonOptions))!.Items).Id);
        using var publicList = await fixture.Client.GetAsync($"/api/v1/schools/{school.Id}/reviews");
        Assert.Empty((await publicList.Content.ReadFromJsonAsync<PagedResult<PublicReviewDto>>())!.Items);
        Assert.Equal(0, (await RatingsAsync(school.Id)).ReviewCount);
        Assert.Null((await RatingsAsync(school.Id)).OverallScore);
        using var queue = await SendAsync(HttpMethod.Get, "/reviews/moderation?status=", admin);
        Assert.Equal(HttpStatusCode.OK, queue.StatusCode);
        Assert.DoesNotContain((await queue.Content.ReadFromJsonAsync<PagedResult<ManagedReviewDto>>(JsonOptions))!.Items, item => item.Id == draft.Id);
        using (var scope = fixture.Services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IReviewRepository>();
            Assert.Equal(0, (await repository.ManagedAsync(null, null, school.Id, 1, default)).TotalCount);
        }
        using var draftQueue = await SendAsync(HttpMethod.Get, "/reviews/moderation?status=Draft", admin);
        Assert.Equal(HttpStatusCode.BadRequest, draftQueue.StatusCode);
        using var adminHistory = await SendAsync(HttpMethod.Get, $"/reviews/{draft.Id}/history", admin);
        using var otherHistory = await SendAsync(HttpMethod.Get, $"/reviews/{draft.Id}/history", other);
        Assert.Equal(HttpStatusCode.NotFound, adminHistory.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherHistory.StatusCode);
        using var approval = await ModerateAsync(draft.Id, 1, true, admin);
        Assert.Equal(HttpStatusCode.Conflict, approval.StatusCode);
        using var duplicate = await SendAsync(HttpMethod.Post, "/reviews", parent,
            new { schoolId = school.Id, content = incomplete, saveAsDraft = true });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var incompleteSubmit = await SendAsync(HttpMethod.Put, $"/reviews/{draft.Id}", parent,
            new { version = 1, content = incomplete });
        Assert.Equal(HttpStatusCode.BadRequest, incompleteSubmit.StatusCode);
        using var ownerHistory = await SendAsync(HttpMethod.Get, $"/reviews/{draft.Id}/history", parent);
        var first = Assert.Single((await ownerHistory.Content.ReadFromJsonAsync<PagedResult<ReviewHistoryDto>>(JsonOptions))!.Items);
        Assert.Equal(ReviewStatus.Draft, first.Status);
        Assert.Equal(1, first.Version);
        using var submitted = await SendAsync(HttpMethod.Put, $"/reviews/{draft.Id}", parent,
            new { version = 1, content = Content(4), saveAsDraft = false });
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var pending = (await submitted.Content.ReadFromJsonAsync<ManagedReviewDto>(JsonOptions))!;
        Assert.Equal(2, pending.Version);
        Assert.Equal(ReviewStatus.PendingModeration, pending.Status);
        using var submittedHistory = await SendAsync(HttpMethod.Get, $"/reviews/{draft.Id}/history", admin);
        var visible = (await submittedHistory.Content.ReadFromJsonAsync<PagedResult<ReviewHistoryDto>>(JsonOptions))!;
        Assert.Equal(1, visible.TotalCount);
        Assert.Equal(2, Assert.Single(visible.Items).Version);
        Assert.Equal(0, (await RatingsAsync(school.Id)).ReviewCount);
        using var published = await ModerateAsync(draft.Id, 2, true, admin);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        Assert.Equal(4d, (await RatingsAsync(school.Id)).OverallScore);
    }

    [RequiresPostgreSql]
    public async Task SavingPublishedReviewAsDraftWithdrawsItAndPrivateHistorySurvivesDeletion()
    {
        var (school, admin, parent, _) = await SeedAsync();
        var review = await CreateAsync(school.Id, parent);
        using var approved = await ModerateAsync(review.Id, 1, true, admin);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var saved = await SendAsync(HttpMethod.Put, $"/reviews/{review.Id}", parent,
            new { version = 1, content = Content(0) with { ReviewText = "Private notes." }, saveAsDraft = true });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var draft = (await saved.Content.ReadFromJsonAsync<ManagedReviewDto>(JsonOptions))!;
        Assert.Equal(ReviewStatus.Draft, draft.Status);
        Assert.Null(draft.ModerationNotes);
        Assert.Equal(0, (await RatingsAsync(school.Id)).ReviewCount);
        using var publicList = await fixture.Client.GetAsync($"/api/v1/schools/{school.Id}/reviews");
        Assert.Empty((await publicList.Content.ReadFromJsonAsync<PagedResult<PublicReviewDto>>())!.Items);
        using var hidden = await SendAsync(HttpMethod.Get, $"/reviews/{review.Id}/history", admin);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        using var staleDelete = await SendAsync(HttpMethod.Delete, $"/reviews/{review.Id}?version=1", parent);
        Assert.Equal(HttpStatusCode.Conflict, staleDelete.StatusCode);
        using var deleted = await SendAsync(HttpMethod.Delete, $"/reviews/{review.Id}?version=2", parent);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var history = await SendAsync(HttpMethod.Get, $"/reviews/{review.Id}/history", parent);
        var versions = (await history.Content.ReadFromJsonAsync<PagedResult<ReviewHistoryDto>>(JsonOptions))!;
        Assert.Equal(2, versions.TotalCount);
        Assert.Equal(new[] { ReviewStatus.Draft, ReviewStatus.Published }, versions.Items.Select(item => item.Status));
        Assert.All(versions.Items, item => Assert.NotNull(item.WithdrawnAt));
        using var recreated = await SendAsync(HttpMethod.Post, "/reviews", parent,
            new { schoolId = school.Id, content = Content(0) with { ReviewText = "" }, saveAsDraft = true });
        Assert.Equal(HttpStatusCode.Created, recreated.StatusCode);
        var restored = (await recreated.Content.ReadFromJsonAsync<ManagedReviewDto>(JsonOptions))!;
        Assert.Equal(review.Id, restored.Id);
        Assert.Equal(3, restored.Version);
        using var submitted = await SendAsync(HttpMethod.Put, $"/reviews/{review.Id}", parent,
            new { version = 3, content = Content(5) });
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        using var adminHistory = await SendAsync(HttpMethod.Get, $"/reviews/{review.Id}/history", admin);
        var visible = (await adminHistory.Content.ReadFromJsonAsync<PagedResult<ReviewHistoryDto>>(JsonOptions))!;
        Assert.Equal(2, visible.TotalCount);
        Assert.Equal(new[] { 4, 1 }, visible.Items.Select(item => item.Version));
        Assert.DoesNotContain(visible.Items, item => item.Status == ReviewStatus.Draft);
    }

    [RequiresPostgreSql]
    public async Task ConcurrentDraftSaveAndSubmitPreserveOneNextVersion()
    {
        var (school, _, parent, _) = await SeedAsync();
        using var created = await SendAsync(HttpMethod.Post, "/reviews", parent,
            new { schoolId = school.Id, content = Content(0) with { ReviewText = "" }, saveAsDraft = true });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var draft = (await created.Content.ReadFromJsonAsync<ManagedReviewDto>(JsonOptions))!;
        var responses = await Task.WhenAll(
            SendAsync(HttpMethod.Put, $"/reviews/{draft.Id}", parent,
                new { version = 1, content = Content(0) with { ReviewText = "Private notes." }, saveAsDraft = true }),
            SendAsync(HttpMethod.Put, $"/reviews/{draft.Id}", parent,
                new { version = 1, content = Content(4), saveAsDraft = false }));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            using var scope = fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
            var current = await db.Reviews.SingleAsync(item => item.Id == draft.Id);
            Assert.Equal(2, current.Version);
            var versions = await db.ReviewVersions.Where(item => item.ReviewId == draft.Id).ToListAsync();
            Assert.Equal(2, versions.Count);
            Assert.Equal(current.Status, versions.Single(item => item.Version == 2).Status);
            Assert.Equal(0, (await RatingsAsync(school.Id)).ReviewCount);
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [RequiresPostgreSql]
    public async Task DatabaseEnforcesDraftRatingBoundsInCurrentReviewsAndHistory()
    {
        var (school, _, parent, _) = await SeedAsync();
        foreach (var (saveAsDraft, rating) in new[] { (false, 0), (true, 6) })
        {
            using var scope = fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
            db.Reviews.Add(new Review(school.Id, parent.Id, Content(rating).ToContent(), saveAsDraft));
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal("ck_reviews_ratings", Assert.IsType<Npgsql.PostgresException>(exception.InnerException).ConstraintName);
        }
        using var versionScope = fixture.Services.CreateScope();
        var versionDb = versionScope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
        var current = new Review(school.Id, parent.Id, Content(0).ToContent());
        var invalidSnapshot = new ReviewVersion(current);
        current.Resubmit(Content(0).ToContent(), saveAsDraft: true);
        versionDb.AddRange(current, invalidSnapshot, new ReviewVersion(current));
        var versionException = await Assert.ThrowsAsync<DbUpdateException>(() => versionDb.SaveChangesAsync());
        Assert.Equal("ck_review_versions_ratings", Assert.IsType<Npgsql.PostgresException>(versionException.InnerException).ConstraintName);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    private static ReviewWriteDto Content(int rating) =>
        new(rating, rating, rating, rating, rating, rating, "Approachable staff", "More outdoor space would help",
            "The weekly classroom updates were helpful and the daily collection routine was clearly explained.");

    private async Task<ManagedReviewDto> CreateAsync(Guid schoolId, User parent, int rating = 4, ReviewWriteDto? input = null)
    {
        using var response = await SendAsync(HttpMethod.Post, "/reviews", parent, new { schoolId, content = input ?? Content(rating) });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ManagedReviewDto>(JsonOptions))!;
    }

    private Task<HttpResponseMessage> ModerateAsync(Guid id, int version, bool approve, User admin) =>
        SendAsync(HttpMethod.Post, $"/reviews/{id}/moderation", admin,
            new { version, approve, notes = approve ? null : "Please add context." });

    private async Task<SchoolRatingsDto> RatingsAsync(Guid schoolId) =>
        (await fixture.Client.GetFromJsonAsync<SchoolRatingsDto>($"/api/v1/schools/{schoolId}/ratings"))!;

    private async Task<(School School, User Admin, User Parent, User Other)> SeedAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolWiseDbContext>();
        var school = new School("Review test school", $"review-{Guid.NewGuid():N}", SchoolType.Elementary, "Test address", "Athens", true);
        school.Update(school.Name, school.Slug, null, school.SchoolType, school.Address, school.City, null,
            null, null, null, null, null, null, null, true, null, true);
        var admin = new User("Test", "Admin", $"{Guid.NewGuid():N}@example.invalid", "unused", UserRole.Admin);
        var parent = new User("Test", "Parent", $"{Guid.NewGuid():N}@example.invalid", "unused");
        var other = new User("Test", "Parent", $"{Guid.NewGuid():N}@example.invalid", "unused");
        db.AddRange(school, admin, parent, other);
        await db.SaveChangesAsync();
        return (school, admin, parent, other);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, User user, object? body = null)
    {
        using var scope = fixture.Services.CreateScope();
        return SendTokenAsync(method, path, scope.ServiceProvider.GetRequiredService<ITokenService>().CreateAccessToken(user), body);
    }

    private async Task<HttpResponseMessage> SendTokenAsync(HttpMethod method, string path, string token, object? body)
    {
        using var request = new HttpRequestMessage(method, $"/api/v1{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await fixture.Client.SendAsync(request);
    }
}
