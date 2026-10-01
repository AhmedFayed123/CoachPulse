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
    public class ProgramExerciseConfiguration : IEntityTypeConfiguration<ProgramExercise>
    {
        public void Configure(EntityTypeBuilder<ProgramExercise> builder)
        {
            builder.ToTable("ProgramExercises");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Program)
                .WithMany()
                .HasForeignKey(x => x.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Sets)
                .IsRequired();

            builder.Property(x => x.Reps)
                .IsRequired();

            builder.Property(x => x.Day)
                .IsRequired();
            }
    }
}
