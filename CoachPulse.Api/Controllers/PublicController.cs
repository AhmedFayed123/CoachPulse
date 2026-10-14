using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CoachPulse.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/public")]
public sealed class PublicController(CoachPulseDbContext db, IMemoryCache cache) : ControllerBase
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase) { "admin", "api", "hangfire", "hubs", "superadmin", "www", "app", "login", "register", "health", "healthz", "readyz", "root", "support", "static", "assets" };
    public static bool IsValidSlug(string? slug) => slug is { Length: >= 3 and <= 30 } && Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$") && !Reserved.Contains(slug);

    [HttpGet("tenants/{slug}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "slug" })]
    public async Task<IActionResult> Tenant(string slug, CancellationToken ct)
    {
        if (!IsValidSlug(slug)) return NotFound();
        var key = $"public-tenant:{slug}";
        if (!cache.TryGetValue(key, out object? value))
        {
            var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.Status == "Active" && !x.IsDeleted, ct);
            if (tenant is null) return NotFound();
            value = new { tenant.Name, tenant.LogoUrl, tenant.PrimaryColor, tenant.SecondaryColor };
            cache.Set(key, value, TimeSpan.FromSeconds(60));
        }
        return Ok(value);
    }

    [HttpGet("tenants/{slug}/plans")]
    public async Task<IActionResult> Plans(string slug, CancellationToken ct)
    {
        if (!IsValidSlug(slug)) return NotFound();
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.Status == "Active" && !x.IsDeleted, ct);
        if (tenant is null) return NotFound();
        var plans = await db.Plans.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenant.Id && x.IsActive).Select(x => new { x.Name, x.Price, x.DurationDays, Features = Array.Empty<string>() }).ToListAsync(ct);
        return Ok(plans);
    }

    [HttpGet("invitations/{token}")]
    public async Task<IActionResult> Invitation(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 500) return NotFound(new { message = "Invitation not found." });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var invitation = await db.ClientInvitations.IgnoreQueryFilters().AsNoTracking().Where(x => x.TokenHash == hash && x.AcceptedAt == null && x.ExpiresAt > DateTime.UtcNow)
            .Join(db.Tenants.IgnoreQueryFilters(), x => x.TenantId, t => t.Id, (x, t) => new { Invitation = x, Tenant = t })
            .Where(x => x.Tenant.Status == "Active" && !x.Tenant.IsDeleted).Select(x => new { x.Invitation.Email, x.Invitation.InvitedName }).FirstOrDefaultAsync(ct);
        if (invitation is null) return NotFound(new { message = "Invitation not found." });
        var parts = invitation.Email.Split('@', 2); var masked = parts.Length == 2 ? $"{(parts[0].Length > 0 ? parts[0][0] : '*')}***@{parts[1]}" : "***";
        return Ok(new { name = invitation.InvitedName, email = masked });
    }

    [HttpGet("slug-available")]
    [EnableRateLimiting("public-slug")]
    public async Task<IActionResult> SlugAvailable([FromQuery] string slug, CancellationToken ct)
    {
        if (!IsValidSlug(slug)) return BadRequest(new { message = "Slug must be 3-30 lowercase letters, numbers, or hyphens, and cannot be reserved." });
        var exists = await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug, ct);
        return Ok(new { slug, available = !exists });
    }
}
