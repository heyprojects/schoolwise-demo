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

        if (!await context.Schools.AnyAsync(cancellationToken))
        {
            context.Schools.AddRange(
                CreateDemo("SchoolWise Athens Kindergarten Demo", "Agia Paraskevi", SchoolType.Kindergarten, true, 3200, 6200, "Demo listing for local development."),
                CreateDemo("SchoolWise Central Athens Nursery Demo", "Athens", SchoolType.Nursery, true, 2800, 5400, "Demo listing for local development."),
                CreateDemo("SchoolWise Marousi Elementary Demo", "Marousi", SchoolType.Elementary, true, 4500, 7800, "Demo listing for local development."),
                CreateDemo("SchoolWise Public Athens Elementary Demo", "Athens", SchoolType.Elementary, false, null, null, "Demo public-school listing; not an official school record."),
                CreateDemo("SchoolWise Pallini Kindergarten Demo", "Pallini", SchoolType.Kindergarten, false, null, null, "Demo public-school listing; not an official school record."));
        }

        await context.SaveChangesAsync(cancellationToken);
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
