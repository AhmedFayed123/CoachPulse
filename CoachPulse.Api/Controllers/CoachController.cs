
using System.Security.Claims;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/coach")]
[Authorize(Policy = "CoachOnly")]
public class CoachController : ControllerBase
{
    private readonly CoachPulseDbContext _context;

    public CoachController(CoachPulseDbContext context)
    {
        _context = context;
    }

    [HttpGet("clients")]
    public async Task<IActionResult> GetClients()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var tenantIdClaim = User.FindFirst("tenantId");
        var roleClaim = User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null ||
            !Guid.TryParse(userIdClaim.Value, out var userId) ||
            tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId) ||
            roleClaim == null)
        {
            return Unauthorized("Invalid user or tenant information.");
        }

        var query = _context.Clients
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (roleClaim.Value == UserRole.Staff.ToString())
        {
            query = query.Where(x => x.CoachId == userId);
        }
        else if (roleClaim.Value != UserRole.Owner.ToString())
        {
            return Forbid();
        }

        var clients = await query
            .OrderBy(x => x.User!.Email)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                Email = x.User!.Email,
                x.CoachId,
                x.Goals,
                x.HealthInfo
            })
            .ToListAsync();

        return Ok(clients);
    }
}