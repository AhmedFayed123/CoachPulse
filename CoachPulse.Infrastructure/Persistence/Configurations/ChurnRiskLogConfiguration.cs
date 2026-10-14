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
    public class ChurnRiskLogConfiguration : IEntityTypeConfiguration<ChurnRiskLog>
    {
        public void Configure(EntityTypeBuilder<ChurnRiskLog> builder)
        {
            builder.ToTable("ChurnRiskLog");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RiskScore)
                .HasPrecision(5, 2);
            builder.Property(x => x.RiskLevel).HasMaxLength(10).IsRequired();
            builder.Property(x => x.TopFactorsJson).IsRequired();
            builder.Property(x => x.ModelVersion).HasMaxLength(100).IsRequired();
            builder.HasIndex(x => new { x.ClientId, x.ComputedAt });
            builder.HasIndex(x => new { x.TenantId, x.RiskLevel, x.ComputedAt });

            builder.HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
