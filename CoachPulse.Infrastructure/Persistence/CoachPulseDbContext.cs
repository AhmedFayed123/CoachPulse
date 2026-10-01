using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoachPulse.Infrastructure.Persistence
{
    internal class CoachPulseDbContext : DbContext
    {
        public CoachPulseDbContext(
       DbContextOptions<CoachPulseDbContext> options)
       : base(options)
        {
        }
    }
}
