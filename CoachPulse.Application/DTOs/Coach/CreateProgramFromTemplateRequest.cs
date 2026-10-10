using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.DTOs.Coach
{
    public class CreateProgramFromTemplateRequest
    {
        public Guid ClientId { get; set; }

        // لو فاضي، هنستخدم اسم القالب.
        public string? Title { get; set; }
    }
}
