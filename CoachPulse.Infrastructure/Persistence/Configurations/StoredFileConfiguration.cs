using CoachPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachPulse.Infrastructure.Persistence.Configurations;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Purpose).IsRequired().HasMaxLength(40);
        builder.Property(x => x.OriginalName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Sha256).IsRequired().HasMaxLength(64);
        builder.Property(x => x.LinkedEntityType).HasMaxLength(80);
        builder.HasIndex(x => new { x.TenantId, x.OwnerUserId, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.LinkedEntityType, x.LinkedEntityId });
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted, x.CreatedAt });
    }
}
