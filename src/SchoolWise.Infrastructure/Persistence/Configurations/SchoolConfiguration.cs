using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Configurations;

public sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("schools", table =>
        {
            table.HasCheckConstraint("ck_schools_tuition_range", "tuition_from IS NULL OR tuition_to IS NULL OR tuition_from <= tuition_to");
            table.HasCheckConstraint("ck_schools_tuition_nonnegative", "(tuition_from IS NULL OR tuition_from >= 0) AND (tuition_to IS NULL OR tuition_to >= 0)");
            table.HasCheckConstraint("ck_schools_student_capacity_positive", "student_capacity IS NULL OR student_capacity > 0");
        });
        builder.HasKey(school => school.Id);
        builder.Property(school => school.Name).HasMaxLength(200).IsRequired();
        builder.Property(school => school.Slug).HasMaxLength(220).IsRequired();
        builder.HasIndex(school => school.Slug).IsUnique();
        builder.Property(school => school.Description).HasColumnType("text");
        builder.Property(school => school.SchoolType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(school => school.Address).HasMaxLength(300).IsRequired();
        builder.Property(school => school.City).HasMaxLength(120).IsRequired();
        builder.Property(school => school.PostalCode).HasMaxLength(16);
        builder.Property(school => school.Phone).HasMaxLength(32);
        builder.Property(school => school.Email).HasMaxLength(320);
        builder.Property(school => school.Website).HasMaxLength(500);
        builder.Property(school => school.TuitionFrom).HasPrecision(10, 2);
        builder.Property(school => school.TuitionTo).HasPrecision(10, 2);
        builder.Property(school => school.LogoUrl).HasMaxLength(1000);
        builder.Property(school => school.CreatedAt).IsRequired();
        builder.HasIndex(school => new { school.City, school.SchoolType, school.IsPrivate, school.IsApproved });
        builder.HasIndex(school => school.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(school => school.City).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(school => school.Address).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(school => school.Description).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasMany(school => school.Photos)
            .WithOne(photo => photo.School)
            .HasForeignKey(photo => photo.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
