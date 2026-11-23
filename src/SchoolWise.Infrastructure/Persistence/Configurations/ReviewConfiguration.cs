using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews", table =>
        {
            table.HasCheckConstraint("ck_reviews_ratings", ReviewContentConfiguration.RatingsConstraint);
            table.HasCheckConstraint("ck_reviews_status", "status IN ('Draft', 'PendingModeration', 'Published', 'Rejected')");
            table.HasCheckConstraint("ck_reviews_version", "version > 0");
        });
        builder.HasKey(review => review.Id);
        builder.HasIndex(review => new { review.UserId, review.SchoolId }).IsUnique();
        builder.HasIndex(review => new { review.SchoolId, review.Status, review.UpdatedAt });
        builder.HasIndex(review => new { review.Status, review.UpdatedAt });
        builder.Property(review => review.Status).HasConversion<string>().HasMaxLength(32).IsConcurrencyToken();
        builder.Property(review => review.Version).IsConcurrencyToken();
        builder.Property(review => review.DeletedAt).IsConcurrencyToken();
        ReviewContentConfiguration.Configure(builder.ComplexProperty(review => review.Content));
        builder.HasOne(review => review.School).WithMany().HasForeignKey(review => review.SchoolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(review => review.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal static class ReviewContentConfiguration
{
    public const string RatingsConstraint = "(status = 'Draft' AND overall_rating BETWEEN 0 AND 5 AND teaching_rating BETWEEN 0 AND 5 AND " +
        "safety_rating BETWEEN 0 AND 5 AND communication_rating BETWEEN 0 AND 5 AND " +
        "facilities_rating BETWEEN 0 AND 5 AND activities_rating BETWEEN 0 AND 5) OR " +
        "(status <> 'Draft' AND overall_rating BETWEEN 1 AND 5 AND teaching_rating BETWEEN 1 AND 5 AND " +
        "safety_rating BETWEEN 1 AND 5 AND communication_rating BETWEEN 1 AND 5 AND " +
        "facilities_rating BETWEEN 1 AND 5 AND activities_rating BETWEEN 1 AND 5)";

    public static void Configure(ComplexPropertyBuilder<ReviewContent> builder)
    {
        builder.Property(content => content.OverallRating).HasColumnName("overall_rating");
        builder.Property(content => content.TeachingRating).HasColumnName("teaching_rating");
        builder.Property(content => content.SafetyRating).HasColumnName("safety_rating");
        builder.Property(content => content.CommunicationRating).HasColumnName("communication_rating");
        builder.Property(content => content.FacilitiesRating).HasColumnName("facilities_rating");
        builder.Property(content => content.ActivitiesRating).HasColumnName("activities_rating");
        builder.Property(content => content.Pros).HasColumnName("pros").HasMaxLength(1000);
        builder.Property(content => content.Cons).HasColumnName("cons").HasMaxLength(1000);
        builder.Property(content => content.ReviewText).HasColumnName("review_text").HasMaxLength(5000).IsRequired();
    }
}
