using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Domain.Entities;
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

    public sealed record TenantStatusRequest(string Reason);
    public sealed record DeleteTenantRequest(string Slug);
    public sealed record ExerciseRequest(string Name, string MuscleGroup, string? Equipment, string? VideoUrl, Guid? VideoFileId);
    private SecurityAuditLog Audit(Guid tenantId, string action, string? oldValue, string newValue) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, UserId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actor) ? actor : null,
        ActorUserId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actorId) ? actorId : null,
        ActorRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value, EventType = action, Action = action,
        EntityType = "Tenant", EntityId = tenantId.ToString(), OldValue = oldValue, NewValue = newValue,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), CreatedAt = DateTime.UtcNow
    };

    [HttpGet("tenants/{id:guid}")]
    public async Task<IActionResult> GetTenant(Guid id, CancellationToken ct)
    {
        var t = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        return t is null ? NotFound() : Ok(new { t.Id, t.Name, t.Slug, t.LogoUrl, t.PrimaryColor, t.SecondaryColor, t.Status, t.CreatedAt, t.DeletionScheduledAt });
    }

    [HttpPut("tenants/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendTenant(Guid id, [FromBody] TenantStatusRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason) || request.Reason.Length > 500) return UnprocessableEntity("A reason is required.");
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (tenant is null) return NotFound();
        tenant.Status = "Suspended";
        var userIds = await _context.Users.IgnoreQueryFilters().Where(u => u.TenantId == id).Select(u => u.Id).ToListAsync(ct);
        var tokens = await _context.RefreshTokens.Where(r => userIds.Contains(r.UserId) && r.RevokedAt == null).ToListAsync(ct);
        foreach (var token in tokens) token.RevokedAt = DateTime.UtcNow;
        _context.SecurityAuditLogs.Add(Audit(id, "tenant.suspended", null, request.Reason));
        await _context.SaveChangesAsync(ct);
        return Ok(new { tenant.Id, tenant.Status });
    }

    [HttpPut("tenants/{id:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(Guid id, [FromBody] TenantStatusRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Reason) || request.Reason.Length > 500) return UnprocessableEntity("A reason is required.");
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (tenant is null) return NotFound();
        var old = tenant.Status; tenant.Status = "Active";
        _context.SecurityAuditLogs.Add(Audit(id, "tenant.activated", old, request.Reason));
        await _context.SaveChangesAsync(ct); return Ok(new { tenant.Id, tenant.Status });
    }

    [HttpDelete("tenants/{id:guid}")]
    public async Task<IActionResult> ScheduleTenantDelete(Guid id, [FromBody] DeleteTenantRequest request, CancellationToken ct)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (tenant is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request?.Slug) || !string.Equals(request.Slug, tenant.Slug, StringComparison.Ordinal)) return UnprocessableEntity("Tenant slug confirmation did not match.");
        tenant.DeletionScheduledAt = DateTime.UtcNow.AddDays(30);
        tenant.IsDeleted = true;
        tenant.Status = "DeletionScheduled";
        var deleteUserIds = await _context.Users.IgnoreQueryFilters().Where(u => u.TenantId == id).Select(u => u.Id).ToListAsync(ct);
        var deleteTokens = await _context.RefreshTokens.Where(t => deleteUserIds.Contains(t.UserId) && t.RevokedAt == null).ToListAsync(ct);
        foreach (var token in deleteTokens) token.RevokedAt = DateTime.UtcNow;
        _context.SecurityAuditLogs.Add(Audit(id, "tenant.deletion.scheduled", null, tenant.DeletionScheduledAt.Value.ToString("O")));
        await _context.SaveChangesAsync(ct); return Accepted(new { tenant.Id, tenant.DeletionScheduledAt });
    }

    [HttpGet("tenants/{id:guid}/usage")]
    public async Task<IActionResult> TenantUsage(Guid id, CancellationToken ct)
    {
        if (!await _context.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == id && !x.IsDeleted, ct)) return NotFound();
        var clients = await _context.Clients.IgnoreQueryFilters().CountAsync(x => x.TenantId == id, ct);
        var files = await _context.StoredFiles.IgnoreQueryFilters().Where(x => x.TenantId == id && !x.IsDeleted).SumAsync(x => (long?)x.SizeBytes, ct) ?? 0;
        var ai = await _context.AiUsages.IgnoreQueryFilters().CountAsync(x => x.TenantId == id && x.CreatedAt >= DateTime.UtcNow.Date, ct);
        return Ok(new { tenantId = id, clients, storageBytes = files, aiRequestsToday = ai });
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs([FromQuery] Guid? tenantId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || from.HasValue && to.HasValue && (to < from || (to.Value - from.Value).TotalDays > 366)) return BadRequest();
        var q = _context.SecurityAuditLogs.IgnoreQueryFilters().AsNoTracking().AsQueryable();
        if (tenantId.HasValue) q = q.Where(x => x.TenantId == tenantId);
        if (from.HasValue) q = q.Where(x => x.CreatedAt >= from);
        if (to.HasValue) q = q.Where(x => x.CreatedAt <= to);
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.Id, x.TenantId, action = x.Action ?? x.EventType, actorUserId = x.ActorUserId ?? x.UserId, x.ActorRole, x.EntityType, entityId = x.EntityId ?? x.Identifier, x.IpAddress, x.CreatedAt }).ToListAsync(ct);
        return Ok(new { page, pageSize, total, items = rows });
    }

    [HttpGet("webhook-events")]
    public async Task<IActionResult> WebhookEvents([FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || from.HasValue && to.HasValue && (to < from || (to.Value - from.Value).TotalDays > 366)) return BadRequest();
        var q = _context.WebhookEvents.AsNoTracking().AsQueryable();
        if (status is "processed") q = q.Where(x => x.ProcessedAtUtc != null); else if (status is "pending") q = q.Where(x => x.ProcessedAtUtc == null); else if (!string.IsNullOrWhiteSpace(status)) return BadRequest("status must be pending or processed.");
        if (from.HasValue) q = q.Where(x => x.ReceivedAtUtc >= from); if (to.HasValue) q = q.Where(x => x.ReceivedAtUtc <= to);
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.ReceivedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.Id, x.GatewayTransactionId, x.GatewayOrderId, x.ReceivedAtUtc, x.ProcessedAtUtc, status = x.ProcessedAtUtc == null ? "pending" : "processed" }).ToListAsync(ct);
        return Ok(new { page, pageSize, total, items = rows });
    }

    [HttpPost("exercises")]
    public async Task<IActionResult> CreateExercise(ExerciseRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200 || string.IsNullOrWhiteSpace(request.MuscleGroup) || request.MuscleGroup.Length > 100 || request.Equipment?.Length > 100) return UnprocessableEntity();
        if (request.VideoUrl is not null && (!Uri.TryCreate(request.VideoUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))) return UnprocessableEntity();
        if (request.VideoFileId.HasValue && !await _context.StoredFiles.IgnoreQueryFilters().AnyAsync(f => f.Id == request.VideoFileId && f.Purpose == "exercise-video" && !f.IsDeleted, ct)) return UnprocessableEntity("Invalid exercise video file.");
        var e = new CoachPulse.Domain.Entities.Exercise { Id = Guid.NewGuid(), TenantId = null, Name = request.Name.Trim(), MuscleGroup = request.MuscleGroup.Trim(), Equipment = request.Equipment?.Trim(), VideoUrl = request.VideoUrl, VideoFileId = request.VideoFileId };
        _context.Exercises.Add(e); await _context.SaveChangesAsync(ct); return Created($"/api/superadmin/exercises/{e.Id}", e);
    }

    [HttpPut("exercises/{id:guid}")]
    public async Task<IActionResult> UpdateExercise(Guid id, ExerciseRequest request, CancellationToken ct)
    {
        var e = await _context.Exercises.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == null && !x.IsDeleted, ct); if (e is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200 || string.IsNullOrWhiteSpace(request.MuscleGroup) || request.MuscleGroup.Length > 100 || request.Equipment?.Length > 100 || request.VideoUrl is not null && (!Uri.TryCreate(request.VideoUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))) return UnprocessableEntity();
        e.Name = request.Name.Trim(); e.MuscleGroup = request.MuscleGroup.Trim(); e.Equipment = request.Equipment?.Trim(); e.VideoUrl = request.VideoUrl; e.VideoFileId = request.VideoFileId; await _context.SaveChangesAsync(ct); return Ok(e);
    }

    [HttpDelete("exercises/{id:guid}")]
    public async Task<IActionResult> DeleteExercise(Guid id, CancellationToken ct)
    {
        var e = await _context.Exercises.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == null && !x.IsDeleted, ct); if (e is null) return NotFound();
        if (await _context.ProgramExercises.AnyAsync(x => x.ExerciseId == id, ct)) return Conflict("Exercise is used by a program.");
        e.IsDeleted = true; await _context.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
    {
        var tenants = await _context.Tenants.Where(x => !x.IsDeleted)
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

    [HttpGet("ai/usage")]
    public async Task<IActionResult> GetAiUsage(CancellationToken ct)
    {
        var from = DateTime.UtcNow.Date;
        var rows = await _context.AiUsages.IgnoreQueryFilters().Where(x => x.CreatedAt >= from)
            .GroupBy(x => x.TenantId).Select(g => new { TenantId = g.Key, UsedToday = g.Count() }).ToListAsync(ct);
        return Ok(rows);
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
