using CoachPulse.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);

    }
}
