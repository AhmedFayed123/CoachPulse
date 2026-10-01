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
    public class AIGeneratedProgramConfiguration : IEntityTypeConfiguration<AIGeneratedProgram>
    {
        public void Configure(EntityTypeBuilder<AIGeneratedProgram> builder)
        {
            builder.ToTable("AIGeneratedPrograms");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Prompt)
                .IsRequired();

            builder.Property(x => x.GeneratedContent)
                .IsRequired();

            builder.Property(x => x.ApprovedByCoach)
                .IsRequired();

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
