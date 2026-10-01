using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class ProgramExercise
    {
        public Guid Id { get; set; }

        public Guid ProgramId { get; set; }

        public Guid ExerciseId { get; set; }

        public int Sets { get; set; }

        public int Reps { get; set; }

        public int Day { get; set; }

        public Program Program { get; set; } = null!;

        public Exercise Exercise { get; set; } = null!;
    }
}
