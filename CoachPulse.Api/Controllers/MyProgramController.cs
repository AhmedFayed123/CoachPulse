
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
                    !p.IsTemplate && p.Status == "Active")
                .OrderByDescending(p => p.StartDate)
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

        [HttpGet("program/today")]
        public async Task<IActionResult> GetToday()
        {
            var client = await CurrentClient();
            if (client is null) return NotFound(new { message = "Client profile not found." });
            var program = await _context.Programs.AsNoTracking().Where(p => p.ClientId == client.Id && p.Status == "Active" && !p.IsTemplate)
                .OrderByDescending(p => p.StartDate).FirstOrDefaultAsync();
            if (program is null) return Ok(new { restDay = true, dayNumber = 0, exercises = Array.Empty<object>() });
            var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == client.TenantId);
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(tenant?.TimeZone ?? "Africa/Cairo"); }
            catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
            var localToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
            var startUtc = DateTime.SpecifyKind(program.StartDate ?? program.CreatedAt, DateTimeKind.Utc);
            var start = TimeZoneInfo.ConvertTimeFromUtc(startUtc, zone).Date;
            var day = (int)(localToday - start).TotalDays + 1;
            var exercises = day < 1 || day > program.DurationDays ? [] : await (
                from pe in _context.ProgramExercises.AsNoTracking()
                join e in _context.Exercises.AsNoTracking() on pe.ExerciseId equals e.Id
                where pe.ProgramId == program.Id && pe.Day == day
                orderby e.Name
                select new { pe.Id, pe.ExerciseId, ExerciseName = e.Name, e.MuscleGroup, e.VideoUrl, pe.Sets, pe.Reps, pe.RestSeconds }
            ).Cast<object>().ToListAsync();
            return Ok(new { program.Id, program.Title, dayNumber = day, restDay = exercises.Count == 0, exercises });
        }

        [HttpGet("programs")]
        public async Task<IActionResult> GetMyPrograms()
        {
            var client = await CurrentClient();
            if (client is null) return NotFound(new { message = "Client profile not found." });
            var programs = await _context.Programs.AsNoTracking().Where(p => p.ClientId == client.Id && !p.IsTemplate && (p.Status == "Active" || p.Status == "Archived"))
                .OrderByDescending(p => p.StartDate).Select(p => new { p.Id, p.Title, p.Status, p.StartDate, p.DurationDays, p.RowVersion }).ToListAsync();
            return Ok(programs);
        }

        private async Task<CoachPulse.Domain.Entities.Client?> CurrentClient()
        {
            if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ||
                !Guid.TryParse(User.FindFirst("tenantId")?.Value, out var tenantId)) return null;
            return await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId && c.TenantId == tenantId);
        }
    }
}
