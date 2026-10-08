using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/superadmin")]
[Authorize(Policy = "SuperAdminOnly")]
public class SuperAdminController : ControllerBase
{
    private readonly CoachPulseDbContext _context;

    public SuperAdminController(
        CoachPulseDbContext context)
    {
        _context = context;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.LogoUrl,
                x.Status,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(tenants);
    }
    [HttpPut("tenants/{id}/suspend")]
    public async Task<IActionResult> SuspendTenant(Guid id)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tenant == null)
            return NotFound("Tenant not found.");

        if (string.Equals(
            tenant.Status,
            "Suspended",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Tenant is already suspended.");
        }

        tenant.Status = "Suspended";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Tenant suspended successfully.",
            TenantId = tenant.Id,
            Status = tenant.Status
        });
    }
    [HttpGet("platform-analytics")]
    public async Task<IActionResult> GetPlatformAnalytics()
    {
        var totalTenants = await _context.Tenants
            .CountAsync();

        var activeTenants = await _context.Tenants
            .CountAsync(x => x.Status == "Active");

        var suspendedTenants = await _context.Tenants
            .CountAsync(x => x.Status == "Suspended");

        var totalUsers = await _context.Users
            .IgnoreQueryFilters()
            .CountAsync();

        var totalClients = await _context.Clients
            .IgnoreQueryFilters()
            .CountAsync();

        return Ok(new
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            SuspendedTenants = suspendedTenants,
            TotalUsers = totalUsers,
            TotalClients = totalClients
        });
    }
}