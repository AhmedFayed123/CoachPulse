using CoachPulse.Application.DTOs.Tenant;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public TenantController(
        CoachPulseDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

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

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == tenantId);

        if (tenant == null)
            return NotFound("Tenant not found.");

        tenant.Name = request.Name.Trim();
        tenant.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl)
            ? null
            : request.LogoUrl.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Tenant branding updated successfully.",
            Tenant = new
            {
                tenant.Id,
                tenant.Name,
                tenant.Slug,
                tenant.LogoUrl,
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
    [Authorize(Policy = "OwnerOnly")]
    [Obsolete("Use GET /api/tenant/dashboard/churn-risk.")]
    public async Task<IActionResult> GetChurnRisk()
    {
        if (Request.Path == "/api/tenant/churn-risk") RouteDeprecation.AddHeaders(Response, "/api/tenant/dashboard/churn-risk");
        return await GetChurnRiskCore();
    }

    [HttpGet("~/api/tenant/dashboard/churn-risk")]
    [Authorize(Policy = "OwnerOnly")]
    public Task<IActionResult> GetChurnRiskCanonical() => GetChurnRiskCore();

    private async Task<IActionResult> GetChurnRiskCore()
    {
        var tenantIdClaim = User.FindFirst("tenantId");

        if (tenantIdClaim == null ||
            !Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return Unauthorized("Tenant not found.");
        }

        var churnRisks = await _context.ChurnRiskLogs
            .AsNoTracking()
            .OrderByDescending(x => x.ComputedAt)
            .Select(x => new
            {
                x.Id,
                x.ClientId,
                ClientEmail = x.Client.User.Email,
                x.RiskScore,
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
        return Ok(new { TenantId = tenantId, ClientCount = clients, AiUsage = 0, StorageBytes = storageBytes });
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
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString(), CreatedAt = DateTime.UtcNow
    });
}
