using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.DTOs.Coach
{
    public class AddProgramExerciseRequest
    {
        public Guid ExerciseId { get; set; }

        public int Sets { get; set; }

        [System.Text.Json.Serialization.JsonConverter(typeof(FlexibleRepsConverter))]
        public string Reps { get; set; } = "";

        public int Day { get; set; }

        public int RestSeconds { get; set; }

        public byte[]? RowVersion { get; set; }
    }
}
