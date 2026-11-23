using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Configurations;

public sealed class ParentSchoolVerificationConfiguration : IEntityTypeConfiguration<ParentSchoolVerification>
{
    public void Configure(EntityTypeBuilder<ParentSchoolVerification> builder)
    {
        builder.ToTable("parent_school_verifications", table =>
            table.HasCheckConstraint("ck_parent_school_verifications_validity", "valid_until > verified_at"));
        builder.HasKey(verification => verification.Id);
        builder.HasIndex(verification => verification.CodeId).IsUnique();
        builder.HasIndex(verification => new { verification.UserId, verification.SchoolId }).IsUnique();
        builder.Property(verification => verification.CodeId).IsConcurrencyToken();
        builder.HasIndex(verification => new { verification.UserId, verification.SchoolId, verification.ValidUntil });
        builder.HasOne(verification => verification.Code).WithMany()
            .HasForeignKey(verification => new { verification.CodeId, verification.SchoolId })
            .HasPrincipalKey(code => new { code.Id, code.SchoolId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(verification => verification.School).WithMany()
            .HasForeignKey(verification => verification.SchoolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(verification => verification.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
