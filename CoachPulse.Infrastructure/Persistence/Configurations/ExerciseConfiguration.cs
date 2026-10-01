using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoachPulse.Domain.Entities;

namespace CoachPulse.Infrastructure.Persistence.Configurations
{
    public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
    {
        public void Configure(EntityTypeBuilder<Exercise> builder)
        {
            builder.ToTable("Exercises");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.MuscleGroup)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.VideoUrl)
                .HasMaxLength(500);
        }
    }
}