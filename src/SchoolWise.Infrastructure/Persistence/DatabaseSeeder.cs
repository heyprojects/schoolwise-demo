using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;
using SchoolWise.Infrastructure.Security;

namespace SchoolWise.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    SchoolWiseDbContext context,
    IPasswordService passwords,
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    public async Task SeedDevelopmentDataAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@schoolwise.local";
        var adminPassword = configuration["Seed:AdminPassword"]
            ?? throw new InvalidOperationException("Set Seed:AdminPassword via user-secrets or environment for development.");
        await EnsureAdminAsync(adminEmail, adminPassword, cancellationToken);

        if (!await context.Schools.AnyAsync(school => school.Name.StartsWith("SchoolWise ")
            && school.Name.EndsWith(" Demo"), cancellationToken))
        {
            context.Schools.AddRange(
                CreateDemo("SchoolWise Athens Kindergarten Demo", "Agia Paraskevi", SchoolType.Kindergarten, true, 3200, 6200, "Demo listing for local development."),
                CreateDemo("SchoolWise Central Athens Nursery Demo", "Athens", SchoolType.Nursery, true, 2800, 5400, "Demo listing for local development."),
                CreateDemo("SchoolWise Marousi Elementary Demo", "Marousi", SchoolType.Elementary, true, 4500, 7800, "Demo listing for local development."),
                CreateDemo("SchoolWise Public Athens Elementary Demo", "Athens", SchoolType.Elementary, false, null, null, "Demo public-school listing; not an official school record."),
                CreateDemo("SchoolWise Pallini Kindergarten Demo", "Pallini", SchoolType.Kindergarten, false, null, null, "Demo public-school listing; not an official school record."));
        }

        await context.SaveChangesAsync(cancellationToken);
        await SeedDemoReviewsAsync(adminEmail, cancellationToken);
    }

    private async Task SeedDemoReviewsAsync(string adminEmail, CancellationToken cancellationToken)
    {
        var schools = await context.Schools.Where(school => school.Name.StartsWith("SchoolWise ")
            && school.Name.EndsWith(" Demo")).ToListAsync(cancellationToken);
        if (schools.Count == 0) return;
        var admin = await context.Users.SingleAsync(user => user.Email == adminEmail.Trim().ToLowerInvariant(), cancellationToken);
        var experiences = new[]
        {
            (5, "Clear weekly updates and a welcoming classroom.", "Pickup parking can be busy.",
                "The weekly learning summaries made it easy to follow classroom activities. Questions about the daily routine received clear and helpful answers."),
            (4, "Varied activities and approachable teaching staff.", "More notice of timetable changes would help.",
                "The mix of reading, creative play and outdoor activities felt balanced. Communication was generally helpful, though a few schedule changes arrived late."),
            (3, "Friendly atmosphere and a convenient location.", "Facilities and afternoon activities could improve.",
                "The everyday routine was well organized and staff were polite. I would have liked clearer information about facilities and more variety in afternoon activities."),
            (4, "Consistent safety procedures and useful meetings.", "The outdoor space is limited.",
                "Arrival and collection procedures were explained clearly. Parent meetings were useful for discussing progress, while the outdoor area felt smaller than expected."),
            (5, "Thoughtful activities and regular updates.", "A longer collection window would help.",
                "Our experience of communication and classroom organization has been positive. This fictional submission is waiting for an administrator to review it."),
            (2, "A useful initial introduction.", "The practical information needs more detail.",
                "This fictional submission needs revision before publication because it does not yet explain the experience clearly enough for another parent.")
        };
        for (var index = 0; index < experiences.Length; index++)
        {
            var email = $"sample-parent-{index + 1}@schoolwise.example.invalid";
            var parent = await context.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
            if (parent is null)
            {
                parent = new User("Sample", $"Parent {index + 1}", email, string.Empty);
                parent.SetPasswordHash(passwords.Hash(parent, Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))));
                context.Users.Add(parent);
                await context.SaveChangesAsync(cancellationToken);
            }
            foreach (var school in schools)
            {
                if (await context.Reviews.AnyAsync(review => review.UserId == parent.Id && review.SchoolId == school.Id, cancellationToken))
                    continue;
                var sample = experiences[index];
                var content = new ReviewContent(sample.Item1, sample.Item1, Math.Min(5, sample.Item1 + 1),
                    sample.Item1, Math.Max(1, sample.Item1 - 1), sample.Item1, sample.Item2, sample.Item3,
                    $"[SAMPLE - fictional test review] {sample.Item4}");
                var review = new Review(school.Id, parent.Id, content);
                var version = new ReviewVersion(review);
                if (index != 4)
                {
                    var approved = index < 4;
                    review.Moderate(1, approved);
                    version.Moderate(admin.Id, approved, approved ? "Synthetic development example." : "Please explain the experience in more detail.");
                }
                context.Reviews.Add(review);
                context.ReviewVersions.Add(version);
            }
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SeedInitialAdminAsync(CancellationToken cancellationToken = default)
    {
        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) && string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Set both Seed:AdminEmail and Seed:AdminPassword to create the initial Admin account.");
        }

        await EnsureAdminAsync(adminEmail, adminPassword, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAdminAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await context.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
        {
            return;
        }

        var admin = new User("SchoolWise", "Admin", normalizedEmail, string.Empty, UserRole.Admin);
        admin.SetPasswordHash(passwords.Hash(admin, password));
        context.Users.Add(admin);
    }

    private static School CreateDemo(
        string name,
        string city,
        SchoolType type,
        bool isPrivate,
        decimal? tuitionFrom,
        decimal? tuitionTo,
        string description)
    {
        var slug = string.Join('-', name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var school = new School(name, slug, type, "Demo address", city, isPrivate);
        school.Update(name, slug, description, type, "Demo address", city, null, null, null, null,
            tuitionFrom, tuitionTo, 250, 2010, isPrivate, null, true);
        return school;
    }
}
