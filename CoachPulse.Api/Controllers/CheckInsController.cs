
using System.Security.Claims;
using CoachPulse.Application.DTOs.Coach;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class CheckInsController : ControllerBase
    {
        private readonly CoachPulseDbContext _context;

        public CheckInsController(CoachPulseDbContext context)
        {
            _context = context;
        }

        // GET: /api/clients/{clientId}/checkins
        // Owner: all clients in the tenant.
        // Staff: assigned clients only.
        [HttpGet("clients/{clientId:guid}/checkins")]
        [Authorize(Policy = "CoachOnly")]
        public async Task<IActionResult> GetClientCheckIns(Guid clientId)
        {
            if (!TryGetIdentity(out var userId, out var tenantId))
                return Unauthorized();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var clientQuery = _context.Clients.Where(c =>
                c.Id == clientId &&
                c.TenantId == tenantId);

            if (role == UserRole.Staff.ToString())
            {
                clientQuery = clientQuery.Where(c =>
                    c.CoachId == userId);
            }
            else if (role != UserRole.Owner.ToString())
            {
                return Forbid();
            }

            var clientExists = await clientQuery.AnyAsync();

            if (!clientExists)
            {
                // Do not disclose whether another tenant's client exists.
                return NotFound(new
                {
                    message = "Client not found."
                });
            }

            var checkIns = await _context.CheckIns
                .AsNoTracking()
                .Where(c =>
                    c.ClientId == clientId &&
                    c.TenantId == tenantId)
                .OrderByDescending(c => c.Date)
                .Select(c => new
                {
                    c.Id,
                    c.ClientId,
                    c.Date,
                    c.Weight,
                    c.Measurements,
                    c.PhotoUrl
                })
                .ToListAsync();

            return Ok(checkIns);
        }

        // POST: /api/my/checkins
        // Client creates a check-in for their own account.
        [HttpPost("my/checkins")]
        [Authorize(Policy = "ClientOnly")]
        public async Task<IActionResult> CreateMyCheckIn(
            [FromBody] CreateCheckInRequest request)
        {
            if (!TryGetIdentity(out var userId, out var tenantId))
                return Unauthorized();

            var client = await _context.Clients
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

            var checkIn = new CheckIn
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ClientId = client.Id,
                Date = DateTime.UtcNow,
                Weight = request.Weight,
                Measurements = request.Measurements?.Trim(),
                PhotoUrl = request.PhotoUrl?.Trim()
            };

            _context.CheckIns.Add(checkIn);
            await _context.SaveChangesAsync();

            return Created(
                $"/api/my/checkins/{checkIn.Id}",
                new
                {
                    message = "Check-in created successfully.",
                    checkIn.Id,
                    checkIn.ClientId,
                    checkIn.Date,
                    checkIn.Weight,
                    checkIn.Measurements,
                    checkIn.PhotoUrl
                });
        }

        // GET: /api/my/checkins
        // Client sees only their own check-ins.
        [HttpGet("my/checkins")]
        [Authorize(Policy = "ClientOnly")]
        public async Task<IActionResult> GetMyCheckIns()
        {
            if (!TryGetIdentity(out var userId, out var tenantId))
                return Unauthorized();

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

            var checkIns = await _context.CheckIns
                .AsNoTracking()
                .Where(c =>
                    c.ClientId == client.Id &&
                    c.TenantId == tenantId)
                .OrderByDescending(c => c.Date)
                .Select(c => new
                {
                    c.Id,
                    c.Date,
                    c.Weight,
                    c.Measurements,
                    c.PhotoUrl
                })
                .ToListAsync();

            return Ok(checkIns);
        }

        private bool TryGetIdentity(
            out Guid userId,
            out Guid tenantId)
        {
            userId = Guid.Empty;
            tenantId = Guid.Empty;

            return Guid.TryParse(
                       User.FindFirst(
                           ClaimTypes.NameIdentifier)?.Value,
                       out userId)
                   && Guid.TryParse(
                       User.FindFirst("tenantId")?.Value,
                       out tenantId);
        }
    }
}
