using System.Security.Claims;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController, Route("api/plans"), Authorize]
public sealed class PlansController(CoachPulseDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!Identity(out var tenant, out var role)) return Unauthorized();
        if (role == UserRole.SuperAdmin) return Forbid();
        var q = db.Plans.AsNoTracking().Where(x => x.TenantId == tenant);
        if (role is UserRole.Client or UserRole.CoachStaff) q = q.Where(x => x.IsActive);
        return Ok(await q.OrderBy(x => x.Price).Select(x => new { x.Id, x.Name, x.Price, x.DurationDays, x.IsActive }).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!Identity(out var tenant, out var role)) return Unauthorized();
        if (role == UserRole.SuperAdmin) return Forbid();
        var q = db.Plans.AsNoTracking().Where(x => x.Id == id && x.TenantId == tenant);
        if (role is UserRole.Client or UserRole.CoachStaff) q=q.Where(x=>x.IsActive);
        var plan = await q.Select(x => new { x.Id, x.Name, x.Price, x.DurationDays, x.IsActive }).FirstOrDefaultAsync(ct);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpPut("~/api/tenant/plans/{id:guid}"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Update(Guid id, PlanUpdate request, CancellationToken ct)
    {
        if (!Identity(out var tenant, out _)) return Unauthorized();
        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant, ct);
        if (plan is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Price < 0 || request.DurationDays is < 1 or > 3650) return UnprocessableEntity();
        plan.Name = request.Name.Trim(); plan.Price = request.Price; plan.DurationDays = request.DurationDays;
        await db.SaveChangesAsync(ct); return Ok(new { plan.Id, plan.Name, plan.Price, plan.DurationDays, plan.IsActive });
    }

    [HttpPut("~/api/tenant/plans/{id:guid}/archive"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        if (!Identity(out var tenant, out _)) return Unauthorized();
        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant, ct);
        if (plan is null) return NotFound(); plan.IsActive = false; await db.SaveChangesAsync(ct); return NoContent();
    }

    private bool Identity(out Guid tenantId, out UserRole role)
    {
        role = default;
        return Guid.TryParse(User.FindFirst("tenantId")?.Value, out tenantId) && Enum.TryParse(User.FindFirst(ClaimTypes.Role)?.Value, out role);
    }
}

public sealed record PlanUpdate(string Name, decimal Price, int DurationDays);
