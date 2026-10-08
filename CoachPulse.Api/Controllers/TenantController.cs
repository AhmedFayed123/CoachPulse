using CoachPulse.Application.DTOs.Tenant;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize(Policy = "OwnerOnly")]
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
    public async Task<IActionResult> GetDashboardSummary()
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
}