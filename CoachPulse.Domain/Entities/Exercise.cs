using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Domain.Entities
{
    public class Exercise
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string MuscleGroup { get; set; } = string.Empty;

        public string? VideoUrl { get; set; }
    }
}
