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
    public class ProgramConfiguration
           : IEntityTypeConfiguration<Program>
    {
        public void Configure(EntityTypeBuilder<Program> builder)
        {
            builder.ToTable("Programs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);
            builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Draft");
            builder.Property(x => x.DurationDays).IsRequired().HasDefaultValue(7);
            builder.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken();
            builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.HasIndex(x => new { x.TenantId, x.ClientId })
                .IsUnique().HasFilter("\"Status\" = 'Active' AND \"ClientId\" IS NOT NULL AND \"IsTemplate\" = false AND \"IsDeleted\" = false");

            builder.HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Coach)
                .WithMany()
                .HasForeignKey(x => x.CoachId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
