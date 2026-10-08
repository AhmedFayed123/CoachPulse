using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.DTOs.Auth
{
    public class RegisterClientRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public Guid TenantId { get; set; }

        public Guid? CoachId { get; set; }

        public string? Goals { get; set; }
        public string? HealthInfo { get; set; }
    }
}
