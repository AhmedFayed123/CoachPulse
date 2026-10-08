using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.DTOs.Tenant
{
    public class UpdateBrandingRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
    }
}
