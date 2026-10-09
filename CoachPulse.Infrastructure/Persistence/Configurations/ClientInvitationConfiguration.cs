using CoachPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachPulse.Infrastructure.Persistence.Configurations;

public class ClientInvitationConfiguration
    : IEntityTypeConfiguration<ClientInvitation>
{
    public void Configure(EntityTypeBuilder<ClientInvitation> builder)
    {
        builder.ToTable("ClientInvitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Email });

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}