using System.Security.Claims;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController, Route("api/tenant/audit-logs"), Authorize(Policy = "OwnerOnly")]
public sealed class TenantAuditController(CoachPulseDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? action, [FromQuery] Guid? actorId, [FromQuery] string? entityType, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!Guid.TryParse(User.FindFirst("tenantId")?.Value, out var tenant)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100 || from.HasValue && to.HasValue && (to < from || (to.Value - from.Value).TotalDays > 366)) return BadRequest("Invalid pagination or date range (maximum 366 days).");
        var q = db.SecurityAuditLogs.AsNoTracking().Where(x => x.TenantId == tenant);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(x => (x.Action ?? x.EventType) == action);
        if (actorId.HasValue) q = q.Where(x => (x.ActorUserId ?? x.UserId) == actorId);
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(x => x.EntityType == entityType);
        if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.Id, action = x.Action ?? x.EventType, actorUserId = x.ActorUserId ?? x.UserId, x.ActorRole, entityType = x.EntityType, entityId = x.EntityId ?? x.Identifier, x.OldValue, x.NewValue, x.IpAddress, x.CreatedAt }).ToListAsync(ct);
        return Ok(new { page, pageSize, total, items = rows });
    }
}
