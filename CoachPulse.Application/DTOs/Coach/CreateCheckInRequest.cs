using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.DTOs.Coach
{
    public class CreateCheckInRequest
    {
        [Range(typeof(decimal), "1", "500")]
        public decimal Weight { get; set; }

        [StringLength(2000)]
        public string? Measurements { get; set; }

        [StringLength(500)]
        public string? PhotoUrl { get; set; }
    }
}
