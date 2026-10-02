using CoachPulse.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string TenantName { get; set; } = string.Empty;

        public string TenantSlug { get; set; } = string.Empty;

        public UserRole Role { get; set; }
    }
}
