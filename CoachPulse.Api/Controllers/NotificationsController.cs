using System.Security.Claims;
using System.Text.Json;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController, Authorize, Route("api/notifications")]
public sealed class NotificationsController(CoachPulseDbContext db) : ControllerBase
{
    private bool Identity(out Guid user, out Guid tenant)
    {
        user = Guid.Empty; tenant = Guid.Empty;
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out user) && Guid.TryParse(User.FindFirst("tenantId")?.Value, out tenant);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool unread = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!Identity(out var user, out var tenant)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100) return UnprocessableEntity();
        var q = db.Notifications.AsNoTracking().Where(x => x.UserId == user && x.TenantId == tenant);
        if (unread) q = q.Where(x => !x.IsRead);
        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.Id, x.Type, x.Title, x.Body, x.DataJson, x.IsRead, x.CreatedAt }).ToListAsync(ct);
        return Ok(new { page, pageSize, total, items = rows });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct) => !Identity(out var user, out var tenant) ? Unauthorized() : Ok(new { count = await db.Notifications.CountAsync(x => x.UserId == user && x.TenantId == tenant && !x.IsRead, ct) });

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct)
    {
        if (!Identity(out var user, out var tenant)) return Unauthorized();
        var row = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user && x.TenantId == tenant, ct);
        if (row is null) return NotFound(); row.IsRead = true; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        if (!Identity(out var user, out var tenant)) return Unauthorized();
        await db.Notifications.Where(x => x.UserId == user && x.TenantId == tenant && !x.IsRead).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct); return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!Identity(out var user, out var tenant)) return Unauthorized();
        var row = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user && x.TenantId == tenant, ct); if (row is null) return NotFound(); row.IsDeleted = true; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> Preferences(CancellationToken ct)
    {
        if (!Identity(out var user, out var tenant)) return Unauthorized();
        return Ok(await db.NotificationPreferences.AsNoTracking().Where(x => x.UserId == user && x.TenantId == tenant).Select(x => new { x.Type, x.InAppEnabled, x.EmailEnabled }).ToListAsync(ct));
    }

    public sealed record PreferenceItem(string Type, bool InAppEnabled, bool EmailEnabled);
    public sealed record PreferenceRequest(List<PreferenceItem> Preferences);

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(PreferenceRequest request, CancellationToken ct)
    {
        if (!Identity(out var user, out var tenant)) return Unauthorized();
        if (request.Preferences is null || request.Preferences.Count > 100 || request.Preferences.Any(x => string.IsNullOrWhiteSpace(x.Type) || x.Type.Length > 80)) return UnprocessableEntity();
        var types = request.Preferences.Select(x => x.Type).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var existing = await db.NotificationPreferences.Where(x => x.UserId == user && x.TenantId == tenant && types.Contains(x.Type)).ToListAsync(ct);
        foreach (var pref in request.Preferences)
        {
            var row = existing.FirstOrDefault(x => x.Type == pref.Type);
            if (row is null) { row = new NotificationPreference { UserId = user, TenantId = tenant, Type = pref.Type }; db.NotificationPreferences.Add(row); }
            var critical = pref.Type is "PaymentStatusChanged" or "SecurityAlert" or "PaymentConfirmed" or "PaymentRejected";
            row.InAppEnabled = critical || pref.InAppEnabled; row.EmailEnabled = critical || pref.EmailEnabled;
        }
        await db.SaveChangesAsync(ct); return Ok(request.Preferences);
    }
}
