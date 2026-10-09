using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.DTOs.Coach
{
    public class CreateProgramRequest
    {
        public Guid ClientId { get; set; }

        public string Title { get; set; } = string.Empty;

        public bool IsTemplate { get; set; }
    }
}
