using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Configurations;

public sealed class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        builder.ToTable("verification_codes", table =>
        {
            table.HasCheckConstraint("ck_verification_codes_validity", "expires_at > created_at AND valid_until >= expires_at");
            table.HasCheckConstraint("ck_verification_codes_redemption",
                "(redeemed_at IS NULL) = (redeemed_by IS NULL)");
            table.HasCheckConstraint("ck_verification_codes_revocation",
                "(revoked_at IS NULL AND revoked_by IS NULL AND revocation_reason IS NULL) OR " +
                "(revoked_at IS NOT NULL AND revoked_by IS NOT NULL AND revocation_reason IS NOT NULL AND length(trim(revocation_reason)) > 0)");
        });
        builder.HasKey(code => code.Id);
        builder.HasAlternateKey(code => new { code.Id, code.SchoolId });
        builder.Property(code => code.SecretHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(code => code.SecretHash).IsUnique();
        builder.HasIndex(code => new { code.SchoolId, code.CreatedAt });
        builder.Property(code => code.RedeemedAt).IsConcurrencyToken();
        builder.Property(code => code.RevokedAt).IsConcurrencyToken();
        builder.Property(code => code.RevocationReason).HasMaxLength(500);
        builder.HasOne<School>().WithMany().HasForeignKey(code => code.SchoolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(code => code.IssuedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(code => code.RedeemedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(code => code.RevokedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
