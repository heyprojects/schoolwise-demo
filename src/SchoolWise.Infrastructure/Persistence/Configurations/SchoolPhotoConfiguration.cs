using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Infrastructure.Persistence.Configurations;

public sealed class SchoolPhotoConfiguration : IEntityTypeConfiguration<SchoolPhoto>
{
    public void Configure(EntityTypeBuilder<SchoolPhoto> builder)
    {
        builder.ToTable("school_photos");
        builder.HasKey(photo => photo.Id);
        builder.Property(photo => photo.Url).HasMaxLength(1000).IsRequired();
        builder.Property(photo => photo.SortOrder).IsRequired();
        builder.HasIndex(photo => new { photo.SchoolId, photo.SortOrder });
    }
}
