
using System.Security.Claims;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers
{
    [ApiController]
    [Route("api/my")]
    [Authorize(Policy = "ClientOnly")]
    public class MyProgramController : ControllerBase
    {
        private readonly CoachPulseDbContext _context;

        public MyProgramController(CoachPulseDbContext context)
        {
            _context = context;
        }

        [HttpGet("program")]
        public async Task<IActionResult> GetMyProgram()
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            var tenantIdValue = User.FindFirst("tenantId")?.Value;

            if (!Guid.TryParse(userIdValue, out var userId) ||
                !Guid.TryParse(tenantIdValue, out var tenantId))
            {
                return Unauthorized(new
                {
                    message = "Invalid user or tenant information."
                });
            }

            // Find the client profile belonging to this user.
            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId &&
                    c.TenantId == tenantId);

            if (client is null)
            {
                return NotFound(new
                {
                    message = "Client profile not found."
                });
            }

            // Templates are not assigned workout programs.
            var program = await _context.Programs
                .AsNoTracking()
                .Where(p =>
                    p.TenantId == tenantId &&
                    p.ClientId == client.Id &&
                    !p.IsTemplate)
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.CoachId,
                    p.ClientId
                })
                .FirstOrDefaultAsync();

            if (program is null)
            {
                return NotFound(new
                {
                    message = "No training program assigned yet."
                });
            }

            // Retrieve the exercises and their training details.
            var exercises = await (
                from pe in _context.ProgramExercises.AsNoTracking()
                join e in _context.Exercises.AsNoTracking()
                    on pe.ExerciseId equals e.Id
                where pe.ProgramId == program.Id
                orderby pe.Day, e.Name
                select new
                {
                    pe.Id,
                    pe.ExerciseId,
                    ExerciseName = e.Name,
                    e.MuscleGroup,
                    e.VideoUrl,
                    pe.Day,
                    pe.Sets,
                    pe.Reps
                }
            ).ToListAsync();

            return Ok(new
            {
                program.Id,
                program.Title,
                program.ClientId,
                program.CoachId,
                Exercises = exercises
            });
        }
    }
}
