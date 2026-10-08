using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.DTOs.Tenant
{
    public class CreatePlanRequest
    {
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int DurationDays { get; set; }
    }
}
