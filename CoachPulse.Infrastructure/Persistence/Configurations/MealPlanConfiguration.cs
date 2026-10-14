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
    public class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
    {
        public void Configure(EntityTypeBuilder<MealPlan> builder)
        {
            builder.ToTable("MealPlans");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Meals).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired().HasDefaultValue("Meal plan");
            builder.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("Draft");
            builder.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().HasDefaultValueSql("gen_random_bytes(8)");
            builder.Property(x => x.IsTemplate).HasDefaultValue(false);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
            builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId).IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.MealPlanId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.TenantId, x.ClientId }).IsUnique().HasFilter("\"Status\" = 'Active' AND \"ClientId\" IS NOT NULL AND \"IsTemplate\" = false AND \"IsDeleted\" = false");
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
