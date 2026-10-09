using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.DTOs.Auth
{
    public class AcceptInvitationRequest
    {
        public string Token { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
