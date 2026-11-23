using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Configurations;

public sealed class ReviewVersionConfiguration : IEntityTypeConfiguration<ReviewVersion>
{
    public void Configure(EntityTypeBuilder<ReviewVersion> builder)
    {
        builder.ToTable("review_versions", table =>
        {
            table.HasCheckConstraint("ck_review_versions_ratings", ReviewContentConfiguration.RatingsConstraint);
            table.HasCheckConstraint("ck_review_versions_status", "status IN ('PendingModeration', 'Published', 'Rejected')");
            table.HasCheckConstraint("ck_review_versions_decision",
                "(status = 'PendingModeration' AND reviewed_at IS NULL AND reviewed_by IS NULL) OR " +
                "(status IN ('Published', 'Rejected') AND reviewed_at IS NOT NULL AND reviewed_by IS NOT NULL)");
            table.HasCheckConstraint("ck_review_versions_version", "version > 0");
        });
        builder.HasKey(version => version.Id);
        builder.HasIndex(version => new { version.ReviewId, version.Version }).IsUnique();
        builder.Property(version => version.Status).HasConversion<string>().HasMaxLength(32).IsConcurrencyToken();
        builder.Property(version => version.WithdrawnAt).IsConcurrencyToken();
        builder.Property(version => version.ModerationNotes).HasMaxLength(1000);
        ReviewContentConfiguration.Configure(builder.ComplexProperty(version => version.Content));
        builder.HasOne<Review>().WithMany().HasForeignKey(version => version.ReviewId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(version => version.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
