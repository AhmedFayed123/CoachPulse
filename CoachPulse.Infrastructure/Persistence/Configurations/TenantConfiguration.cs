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
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            builder.Property(x => x.LogoUrl)
                .HasMaxLength(500);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("EGP");
            builder.Property(x => x.TimeZone).IsRequired().HasMaxLength(100).HasDefaultValue("Africa/Cairo");
            builder.Property(x => x.ReminderDays).IsRequired().HasDefaultValue(7);
            builder.Property(x => x.DefaultLanguage).IsRequired().HasMaxLength(16).HasDefaultValue("ar");
            builder.Property(x => x.InstaPayId).HasMaxLength(64);
            builder.Property(x => x.InstaPayAccountNumber).HasMaxLength(34);

            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}
