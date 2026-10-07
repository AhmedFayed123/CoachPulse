using System.Security.Claims;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly CoachPulseDbContext _context;

    public AccountController(CoachPulseDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return NotFound("User not found.");

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Role,
            user.TenantId,
            Tenant = user.Tenant == null
                ? null
                : new
                {
                    user.Tenant.Id,
                    user.Tenant.Name,
                    user.Tenant.Slug,
                    user.Tenant.Status
                }
        });
    }
}