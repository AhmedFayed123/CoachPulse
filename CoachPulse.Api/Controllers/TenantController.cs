using CoachPulse.Application.DTOs.Tenant;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly CoachPulseDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClientAccessService _clientAccess;
    private readonly IMemoryCache? _memoryCache;
    public TenantController(
        CoachPulseDbContext context,
        IPasswordHasher passwordHasher, IClientAccessService clientAccess, IMemoryCache? memoryCache = null)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _clientAccess = clientAccess;
        _memoryCache = memoryCache;
    }

    public TenantController(CoachPulseDbContext context, IPasswordHasher passwordHasher)
        : this(context, passwordHasher, new CoachPulse.Infrastructure.Services.ClientAccessService(context), null) { }

    public sealed record UpdateCoachRequest(string? Name, string? Email);

    [HttpGet("coaches/{id:guid}")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetCoach(Guid id, CancellationToken ct)
    {
        if (!TenantUser(out var tenantId)) return Unauthorized();
        var user = await _context.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.Role == UserRole.Staff && !x.IsDeleted, ct);
        return user is null ? NotFound() : Ok(new { user.Id, user.Email, user.Name, user.IsActive, user.TenantId });
    }

    [HttpPut("coaches/{id:guid}")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> UpdateCoach(Guid id, UpdateCoachRequest request, CancellationToken ct)
    {
        if (!TenantUser(out var tenantId)) return Unauthorized();
        if (request.Name?.Length > 150 || request.Email?.Length > 255 || request.Email is not null && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email)) return UnprocessableEntity();
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.Role == UserRole.Staff && !x.IsDeleted, ct);
        if (user is null) return NotFound();
        if (request.Email is not null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await _context.Users.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Id != id && x.Email == email && !x.IsDeleted, ct)) return Conflict("Email already exists in this tenant.");
            user.Email = email;
        }
        if (request.Name is not null) user.Name = request.Name.Trim();
        await _context.SaveChangesAsync(ct); return Ok(new { user.Id, user.Email, user.Name, user.IsActive });
    }

    [HttpPut("coaches/{id:guid}/deactivate")]
    [Authorize(Policy = "OwnerOnly")]
    public Task<IActionResult> DeactivateCoach(Guid id, CancellationToken ct) => SetCoachActive(id, false, ct);

    [HttpPut("coaches/{id:guid}/activate")]
    [Authorize(Policy = "OwnerOnly")]
    public Task<IActionResult> ActivateCoach(Guid id, CancellationToken ct) => SetCoachActive(id, true, ct);

    private async Task<IActionResult> SetCoachActive(Guid id, bool active, CancellationToken ct)
    {
        if (!TenantUser(out var tenantId)) return Unauthorized();
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.Role == UserRole.Staff && !x.IsDeleted, ct);
        if (user is null) return NotFound();
        user.IsActive = active;
        if (!active)
        {
            var tokens = await _context.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null).ToListAsync(ct);
            foreach (var token in tokens) token.RevokedAt = DateTime.UtcNow;
        }
        AddAudit(Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : Guid.Empty, tenantId, active ? "coach.activated" : "coach.deactivated", $"coach={id}");
        await _context.SaveChangesAsync(ct); return Ok(new { user.Id, user.IsActive });
    }

    [HttpGet("coaches/{id:guid}/clients")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> CoachClients(Guid id, CancellationToken ct)
    {
        if (!TenantUser(out var tenantId)) return Unauthorized();
        if (!await _context.Users.IgnoreQueryFilters().AnyAsync(x => x.Id == id && x.TenantId == tenantId && x.Role == UserRole.Staff && !x.IsDeleted, ct)) return NotFound();
        return Ok(await _context.Clients.AsNoTracking().Where(x => x.TenantId == tenantId && x.CoachId == id).Select(x => new { x.Id, x.UserId, x.Status }).ToListAsync(ct));
    }

    private bool TenantUser(out Guid tenantId) => Guid.TryParse(User.FindFirst("tenantId")?.Value, out tenantId);

    [HttpGet("dashboard/pending-actions"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> PendingActions(CancellationToken ct)
    {
        if (!TenantUser(out var tenant)) return Unauthorized();
        var pendingCheckIns = await _context.CheckIns.CountAsync(x => x.TenantId == tenant && !x.IsDeleted && x.ReviewedAt == null, ct);
        var pendingPayments = await _context.Payments.CountAsync(x => x.TenantId == tenant && x.Status == PaymentStatus.PendingVerification, ct);
        var pendingInvitations = await _context.ClientInvitations.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenant && x.AcceptedAt == null && x.ExpiresAt > DateTime.UtcNow, ct);
        return Ok(new { pendingCheckIns, pendingPayments, pendingInvitations, total = pendingCheckIns + pendingPayments + pendingInvitations });
    }

    [HttpGet("dashboard/expiring-subscriptions"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> ExpiringSubscriptions([FromQuery] int days = 30, CancellationToken ct = default)
    {
        if (!TenantUser(out var tenant)) return Unauthorized(); if (days is < 1 or > 366) return UnprocessableEntity();
        var now = DateTime.UtcNow;
        var rows = await _context.Subscriptions.AsNoTracking().Where(s => s.TenantId == tenant && s.Status == SubscriptionStatus.Active && s.EndDate >= now && s.EndDate <= now.AddDays(days))
            .OrderBy(s => s.EndDate).Select(s => new { s.Id, s.ClientId, clientName = s.Client.User.Name, s.EndDate, s.SnapshotPrice, s.PlanId }).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("dashboard/clients-activity"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> ClientsActivity([FromQuery] int days = 30, CancellationToken ct = default)
    {
        if (!TenantUser(out var tenant)) return Unauthorized(); if (days is < 1 or > 366) return UnprocessableEntity();
        var since = DateTime.UtcNow.AddDays(-days);
        var rows = await _context.Clients.AsNoTracking().Where(c => c.TenantId == tenant).Select(c => new
        {
            c.Id, c.Status, c.CoachId,
            lastCheckInAt = _context.CheckIns.Where(i => i.TenantId == tenant && i.ClientId == c.Id && !i.IsDeleted).OrderByDescending(i => i.Date).Select(i => (DateTime?)i.Date).FirstOrDefault(),
            recentCheckIns = _context.CheckIns.Count(i => i.TenantId == tenant && i.ClientId == c.Id && !i.IsDeleted && i.Date >= since)
        }).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("dashboard/revenue"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        if (!TenantUser(out var tenant)) return Unauthorized(); if (!ValidRange(from, to)) return BadRequest("Date range must be valid and no longer than 366 days.");
        var payments = _context.Payments.AsNoTracking().Where(p => p.TenantId == tenant && (p.Status == PaymentStatus.Confirmed || p.Status == PaymentStatus.PartiallyRefunded || p.Status == PaymentStatus.Refunded));
        var refunds = _context.PaymentRefunds.AsNoTracking().Where(r => r.TenantId == tenant);
        if (from.HasValue) { payments = payments.Where(p => p.CreatedAt >= from); refunds = refunds.Where(r => r.CreatedAt >= from); }
        if (to.HasValue) { payments = payments.Where(p => p.CreatedAt <= to); refunds = refunds.Where(r => r.CreatedAt <= to); }
        var paid = await payments.GroupBy(p => new { p.Currency, p.Method }).Select(g => new { g.Key.Currency, g.Key.Method, Gross = g.Sum(p => p.Amount) }).ToListAsync(ct);
        var refundRows = await refunds.Select(r => new { r.PaymentId, r.Amount, r.Currency }).ToListAsync(ct);
        var methodByPayment = await payments.Select(p => new { p.Id, p.Method }).ToDictionaryAsync(p => p.Id, p => p.Method, ct);
        var refunded = refundRows.Where(r => methodByPayment.ContainsKey(r.PaymentId)).GroupBy(r => new { r.Currency, Method = methodByPayment[r.PaymentId] }).ToDictionary(g => (g.Key.Currency, g.Key.Method), g => g.Sum(x => x.Amount));
        return Ok(paid.Select(x => new { x.Currency, x.Method, x.Gross, Refunded = refunded.GetValueOrDefault((x.Currency, x.Method)), Net = x.Gross - refunded.GetValueOrDefault((x.Currency, x.Method)) }));
    }

    [HttpGet("analytics/retention"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Retention(CancellationToken ct)
    {
        if (!TenantUser(out var tenant)) return Unauthorized();
        var rows = await _context.Subscriptions.AsNoTracking().Where(x => x.TenantId == tenant).GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        var total = rows.Sum(x => x.Count); var retained = rows.Where(x => x.Status is SubscriptionStatus.Active or SubscriptionStatus.Paused).Sum(x => x.Count);
        return Ok(new { totalSubscriptions = total, retainedSubscriptions = retained, retentionRate = total == 0 ? (decimal?)null : (decimal)retained / total, definition = "Active or paused subscriptions divided by all tenant subscriptions." });
    }

    [HttpGet("analytics/plans-performance"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> PlansPerformance(CancellationToken ct)
    {
        if (!TenantUser(out var tenant)) return Unauthorized();
        var rows = await _context.Plans.AsNoTracking().Where(p => p.TenantId == tenant).Select(p => new
        {
            p.Id, p.Name, p.Price, p.DurationDays,
            subscriptions = _context.Subscriptions.Count(s => s.TenantId == tenant && s.PlanId == p.Id),
            activeSubscriptions = _context.Subscriptions.Count(s => s.TenantId == tenant && s.PlanId == p.Id && s.Status == SubscriptionStatus.Active),
            confirmedRevenue = _context.Payments.Where(pay => pay.TenantId == tenant && pay.Subscription.PlanId == p.Id && (pay.Status == PaymentStatus.Confirmed || pay.Status == PaymentStatus.PartiallyRefunded || pay.Status == PaymentStatus.Refunded)).Sum(pay => (decimal?)pay.Amount) ?? 0m
        }).ToListAsync(ct);
        return Ok(rows);
    }

    private static bool ValidRange(DateTime? from, DateTime? to) => !from.HasValue || !to.HasValue || to.Value >= from.Value && (to.Value - from.Value).TotalDays <= 366;

    [HttpPut("branding")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> UpdateBranding(
        UpdateBrandingRequest request)
    {
        var tenantIdClaim = User.FindFirst("tenantId");

        if (tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return Unauthorized("Tenant not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Tenant name is required.");
        }
        if (request.PrimaryColor is not null && !Regex.IsMatch(request.PrimaryColor, "^#[0-9a-fA-F]{6}$") || request.SecondaryColor is not null && !Regex.IsMatch(request.SecondaryColor, "^#[0-9a-fA-F]{6}$")) return UnprocessableEntity("Brand colors must be six-digit hex values.");

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == tenantId);

        if (tenant == null)
            return NotFound("Tenant not found.");

        tenant.Name = request.Name.Trim();
        tenant.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl)
            ? null
            : request.LogoUrl.Trim();
        tenant.PrimaryColor = request.PrimaryColor?.Trim();
        tenant.SecondaryColor = request.SecondaryColor?.Trim();

        await _context.SaveChangesAsync();
        _memoryCache?.Remove($"public-tenant:{tenant.Slug}");

        return Ok(new
        {
            Message = "Tenant branding updated successfully.",
            Tenant = new
            {
                tenant.Id,
                tenant.Name,
                tenant.Slug,
                tenant.LogoUrl,
                tenant.PrimaryColor,
                tenant.SecondaryColor,
                tenant.Status
            }
        });
    }
    [HttpGet("coaches")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetCoaches()
    {
        var tenantIdClaim = User.FindFirst("tenantId");

        if (tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return Unauthorized("Tenant not found.");
        }

        var coaches = await _context.Users
            .Where(x =>
                x.TenantId == tenantId &&
                (x.Role == UserRole.Owner ||
                 x.Role == UserRole.Staff))
            .AsNoTracking()
            .OrderBy(x => x.Email)
            .Select(x => new
            {
                x.Id,
                x.Email,
                Role = x.Role.ToString(),
                x.TenantId
            })
            .ToListAsync();

        return Ok(coaches);
    }
    [HttpPost("coaches")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> CreateCoach(
    CreateCoachRequest request)
    {
        var tenantIdClaim = User.FindFirst("tenantId");

        if (tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return Unauthorized("Tenant not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Password is required.");

        var email = request.Email.Trim().ToLower();

        var existingUser = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Email == email);

        if (existingUser != null)
        {
            return BadRequest(
                "Email already exists in this tenant.");
        }

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == tenantId);

        if (tenant == null)
            return NotFound("Tenant not found.");

        if (!string.Equals(
            tenant.Status,
            "Active",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Tenant is not active.");
        }

        var coach = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Staff
        };

        _context.Users.Add(coach);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Coach created successfully.",
            Coach = new
            {
                coach.Id,
                coach.Email,
                Role = coach.Role.ToString(),
                coach.TenantId
            }
        });
    }
    [HttpGet("plans")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetPlans()
    {
        var tenantIdClaim = User.FindFirst("tenantId");

        if (tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return Unauthorized("Tenant not found.");
        }

        var plans = await _context.Plans
            .AsNoTracking()
            .OrderBy(x => x.Price)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Price,
                x.DurationDays,
                x.TenantId
            })
            .ToListAsync();

        return Ok(plans);
    }
    [HttpPost("plans")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> CreatePlan(CreatePlanRequest request)
    {
        var tenantIdClaim = User.FindFirst("tenantId");

        if (tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return Unauthorized("Tenant not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Plan name is required.");

        if (request.Price < 0)
            return BadRequest("Plan price cannot be negative.");

        if (request.DurationDays <= 0)
            return BadRequest("Duration days must be greater than zero.");

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == tenantId);

        if (tenant == null)
            return NotFound("Tenant not found.");

        if (!string.Equals(
            tenant.Status,
            "Active",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Tenant is not active.");
        }

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Price = request.Price,
            DurationDays = request.DurationDays
        };

        _context.Plans.Add(plan);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Plan created successfully.",
            Plan = new
            {
                plan.Id,
                plan.Name,
                plan.Price,
                plan.DurationDays,
                plan.TenantId
            }
        });
    }
    [HttpGet("dashboard-summary")]
    [Authorize(Policy = "OwnerOnly")]
    [Obsolete("Use GET /api/tenant/dashboard/summary.")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        if (Request.Path == "/api/tenant/dashboard-summary") RouteDeprecation.AddHeaders(Response, "/api/tenant/dashboard/summary");
        return await GetDashboardSummaryCore();
    }

    [HttpGet("~/api/tenant/dashboard/summary")]
    [Authorize(Policy = "OwnerOnly")]
    public Task<IActionResult> GetDashboardSummaryCanonical() => GetDashboardSummaryCore();

    private async Task<IActionResult> GetDashboardSummaryCore()
    {
        var tenantIdClaim = User.FindFirst("tenantId");

        if (tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return Unauthorized("Tenant not found.");
        }

        var totalClients = await _context.Clients
            .CountAsync();

        var totalCoaches = await _context.Users
            .CountAsync(x =>
                x.Role == UserRole.Owner ||
                x.Role == UserRole.Staff);

        var totalPrograms = await _context.Programs
            .CountAsync();

        var totalMealPlans = await _context.MealPlans
            .CountAsync();

        var totalCheckIns = await _context.CheckIns
            .CountAsync();

        var totalPlans = await _context.Plans
            .CountAsync();

        var totalSubscriptions = await _context.Subscriptions
            .CountAsync();

        var totalPayments = await _context.Payments
            .CountAsync();

        return Ok(new
        {
            TenantId = tenantId,
            TotalClients = totalClients,
            TotalCoaches = totalCoaches,
            TotalPrograms = totalPrograms,
            TotalMealPlans = totalMealPlans,
            TotalCheckIns = totalCheckIns,
            TotalPlans = totalPlans,
            TotalSubscriptions = totalSubscriptions,
            TotalPayments = totalPayments
        });
    }
    [HttpGet("churn-risk")]
    [Authorize(Policy = "CoachOnly")]
    [Obsolete("Use GET /api/tenant/dashboard/churn-risk.")]
    public async Task<IActionResult> GetChurnRisk()
    {
        if (Request.Path == "/api/tenant/churn-risk") RouteDeprecation.AddHeaders(Response, "/api/tenant/dashboard/churn-risk");
        return await GetChurnRiskCore();
    }

    [HttpGet("~/api/tenant/dashboard/churn-risk")]
    [Authorize(Policy = "CoachOnly")]
    public Task<IActionResult> GetChurnRiskCanonical() => GetChurnRiskCore();

    private async Task<IActionResult> GetChurnRiskCore()
    {
        var tenantIdClaim = User.FindFirst("tenantId");

        if (tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return Unauthorized("Tenant not found.");
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || !Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role)) return Unauthorized();
        var visible = _clientAccess.VisibleClients(tenantId, userId, role).Select(c => c.Id);
        var latestIds = await _context.ChurnRiskLogs.AsNoTracking().Where(x => x.TenantId == tenantId && visible.Contains(x.ClientId))
            .GroupBy(x => x.ClientId).Select(g => g.OrderByDescending(x => x.ComputedAt).Select(x => x.Id).First()).ToListAsync();
        var churnRisks = await _context.ChurnRiskLogs
            .AsNoTracking()
            .Where(x => latestIds.Contains(x.Id))
            .OrderByDescending(x => x.RiskScore).ThenByDescending(x => x.ComputedAt)
            .Select(x => new
            {
                x.Id,
                x.ClientId,
                ClientEmail = x.Client.User.Email,
                x.RiskScore,
                x.RiskLevel,
                x.ComputedAt
            })
            .ToListAsync();

        return Ok(churnRisks);
    }

    [HttpGet]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> GetTenant()
    {
        if (!TryTenant(out var tenantId, out var userId, out var role)) return Unauthorized();
        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant is null) return NotFound();
        return Ok(new
        {
            tenant.Id, tenant.Name, tenant.Slug, tenant.LogoUrl, tenant.Status,
            tenant.Currency, tenant.TimeZone, tenant.ReminderDays, tenant.DefaultLanguage,
            PaymentSettings = role == UserRole.Owner ? new { tenant.PaymobEnabled, tenant.InstaPayEnabled, tenant.InstaPayId, tenant.InstaPayAccountNumber } : null
        });
    }

    [HttpPut("settings")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> UpdateSettings(UpdateTenantSettingsRequest request)
    {
        if (!TryTenant(out var tenantId, out var userId, out _)) return Unauthorized();
        var currency = (request.Currency ?? string.Empty).Trim().ToUpperInvariant();
        if (!Regex.IsMatch(currency, "^[A-Z]{3}$")) return BadRequest("Currency must be a three-letter ISO code.");
        if (request.ReminderDays is < 1 or > 14) return BadRequest("ReminderDays must be between 1 and 14.");
        if (string.IsNullOrWhiteSpace(request.TimeZone) || !TimeZoneInfo.TryConvertIanaIdToWindowsId(request.TimeZone.Trim(), out _))
            return BadRequest("TimeZone must be a valid IANA time zone.");
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone.Trim()); }
        catch (TimeZoneNotFoundException) { return BadRequest("TimeZone must be a valid IANA time zone."); }
        catch (InvalidTimeZoneException) { return BadRequest("TimeZone must be a valid IANA time zone."); }
        if (string.IsNullOrWhiteSpace(request.DefaultLanguage) || request.DefaultLanguage.Trim().Length > 16) return BadRequest("DefaultLanguage is required and must be at most 16 characters.");
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant is null) return NotFound();
        if (!string.Equals(tenant.Currency, currency, StringComparison.OrdinalIgnoreCase) &&
            (await _context.Subscriptions.AnyAsync(x => x.TenantId == tenantId) || await _context.Payments.AnyAsync(x => x.TenantId == tenantId)))
            return Conflict("Currency cannot change after subscriptions or payments exist.");
        tenant.Currency = currency; tenant.TimeZone = request.TimeZone.Trim(); tenant.ReminderDays = request.ReminderDays; tenant.DefaultLanguage = request.DefaultLanguage.Trim();
        AddAudit(userId, tenantId, "tenant.settings.changed", "currency,timezone,reminderDays,defaultLanguage");
        await _context.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("payment-settings")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetPaymentSettings()
    {
        if (!TryTenant(out var tenantId, out _, out _)) return Unauthorized();
        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId);
        return tenant is null ? NotFound() : Ok(new { tenant.PaymobEnabled, tenant.InstaPayEnabled, tenant.InstaPayId, tenant.InstaPayAccountNumber });
    }

    [HttpPut("payment-settings")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> UpdatePaymentSettings(UpdatePaymentSettingsRequest request)
    {
        if (!TryTenant(out var tenantId, out var userId, out _)) return Unauthorized();
        var instaPayId = string.IsNullOrWhiteSpace(request.InstaPayId) ? null : request.InstaPayId.Trim();
        var accountNumber = string.IsNullOrWhiteSpace(request.InstaPayAccountNumber) ? null : request.InstaPayAccountNumber.Trim();
        if (instaPayId is not null && !Regex.IsMatch(instaPayId, "^[A-Za-z0-9._@+-]{3,64}$")) return BadRequest("InstaPayId has an invalid format.");
        if (accountNumber is not null && !Regex.IsMatch(accountNumber, "^[0-9]{10,34}$")) return BadRequest("InstaPayAccountNumber must contain 10 to 34 digits.");
        if (request.InstaPayEnabled && instaPayId is null && accountNumber is null) return BadRequest("An InstaPay ID or account number is required when InstaPay is enabled.");
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId);
        if (tenant is null) return NotFound();
        if (!request.PaymobEnabled && !request.InstaPayEnabled && await _context.Subscriptions.AnyAsync(x => x.TenantId == tenantId && x.Status == SubscriptionStatus.Pending))
            return Conflict("At least one payment method must remain enabled while pending subscriptions exist.");
        var changed = tenant.PaymobEnabled != request.PaymobEnabled || tenant.InstaPayEnabled != request.InstaPayEnabled || tenant.InstaPayId != instaPayId || tenant.InstaPayAccountNumber != accountNumber;
        tenant.PaymobEnabled = request.PaymobEnabled; tenant.InstaPayEnabled = request.InstaPayEnabled; tenant.InstaPayId = instaPayId; tenant.InstaPayAccountNumber = accountNumber;
        if (changed) AddAudit(userId, tenantId, "tenant.payment-settings.changed", $"paymob={request.PaymobEnabled};instapay={request.InstaPayEnabled}");
        await _context.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("usage")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Usage()
    {
        if (!TryTenant(out var tenantId, out _, out _)) return Unauthorized();
        var clients = await _context.Clients.CountAsync(x => x.TenantId == tenantId);
        var storageBytes = await _context.StoredFiles.Where(x => x.TenantId == tenantId && !x.IsDeleted).SumAsync(x => (long?)x.SizeBytes) ?? 0L;
        var today = DateTime.UtcNow.Date;
        var aiUsage = await _context.AiUsages.CountAsync(x => x.TenantId == tenantId && x.CreatedAt >= today && x.CreatedAt < today.AddDays(1));
        return Ok(new { TenantId = tenantId, ClientCount = clients, AiUsage = aiUsage, StorageBytes = storageBytes });
    }

    [HttpGet("~/api/tenant/analytics/coach-performance"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> CoachPerformance(CancellationToken ct)
    {
        if (!TryTenant(out var tenantId, out _, out _)) return Unauthorized();
        var activeClients = await _context.Clients.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status == ClientStatus.Active).Select(x => x.Id).ToListAsync(ct);
        var windowStart = DateTime.UtcNow.AddDays(-30); var windowEnd = DateTime.UtcNow;
        var adherenceValues = new List<decimal>();
        foreach (var clientId in activeClients)
        {
            var adherence = await CoachPulse.Api.Services.WorkoutAdherenceCalculator.CalculateAsync(_context, tenantId, clientId, windowStart, windowEnd, ct);
            if (adherence.Rate.HasValue) adherenceValues.Add(adherence.Rate.Value);
        }
        var cohortStart = DateTime.UtcNow.AddDays(-90);
        var cohortClients = await _context.Subscriptions.AsNoTracking().Where(x => x.TenantId == tenantId && x.StartDate >= cohortStart && x.Status != SubscriptionStatus.Pending).Select(x => x.ClientId).Distinct().ToListAsync(ct);
        var retained = cohortClients.Count == 0 ? 0 : await _context.Subscriptions.AsNoTracking().Where(x => x.TenantId == tenantId && cohortClients.Contains(x.ClientId) && x.Status == SubscriptionStatus.Active).Select(x => x.ClientId).Distinct().CountAsync(ct);
        return Ok(new
        {
            periodDays = 30,
            activeClients = activeClients.Count,
            adherenceRate = adherenceValues.Count == 0 ? (decimal?)null : adherenceValues.Average(),
            adherenceClientsIncluded = adherenceValues.Count,
            retentionPeriodDays = 90,
            subscriptionRetentionRate = cohortClients.Count == 0 ? (decimal?)null : (decimal)retained / cohortClients.Count,
            retentionCohortClients = cohortClients.Count
        });
    }

    private bool TryTenant(out Guid tenantId, out Guid userId, out UserRole role)
    {
        tenantId = userId = Guid.Empty; role = default;
        return Guid.TryParse(User.FindFirst("tenantId")?.Value, out tenantId) &&
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId) &&
            Enum.TryParse(User.FindFirst(ClaimTypes.Role)?.Value, out role) && role is UserRole.Owner or UserRole.Staff;
    }

    private void AddAudit(Guid userId, Guid tenantId, string eventType, string identifier) => _context.SecurityAuditLogs.Add(new SecurityAuditLog
    {
        Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, EventType = eventType, Identifier = identifier,
        ActorUserId = userId, ActorRole = User.FindFirst(ClaimTypes.Role)?.Value, Action = eventType,
        EntityType = eventType.StartsWith("coach.", StringComparison.Ordinal) ? "Coach" : "Tenant",
        EntityId = identifier.Contains('=') ? identifier[(identifier.IndexOf('=') + 1)..] : identifier,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString(), CreatedAt = DateTime.UtcNow
    });
}
