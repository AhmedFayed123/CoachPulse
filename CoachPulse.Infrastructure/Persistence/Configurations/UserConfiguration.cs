using CoachPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TenantId)
                .IsRequired(false);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(255);

            // Unique email inside each tenant
            builder.HasIndex(x => new
            {
                x.TenantId,
                x.Email
            })
            .IsUnique()
            .HasFilter("\"TenantId\" IS NOT NULL");

            // SuperAdmin emails must be globally unique
            builder.HasIndex(x => x.Email)
                .IsUnique()
                .HasFilter("\"TenantId\" IS NULL");

            builder.Property(x => x.PasswordHash)
                .IsRequired();

            builder.Property(x => x.Role)
                .IsRequired();

            builder.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
