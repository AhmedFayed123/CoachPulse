using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoachPulse.Api.Infrastructure;
using CoachPulse.Application.DTOs.Clients;
using CoachPulse.Application.DTOs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public sealed class ClientsController(CoachPulseDbContext db, IClientAccessService access, IWebHostEnvironment environment, IEmailSender emailSender) : ControllerBase
{
    private bool TryScope(out Guid tenantId, out Guid userId, out UserRole role)
    {
        tenantId = userId = Guid.Empty; role = default;
        return Guid.TryParse(User.FindFirstValue("tenantId"), out tenantId) &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) &&
            Enum.TryParse(User.FindFirstValue(ClaimTypes.Role), out role);
    }

    private IQueryable<Client> Visible(Guid tenantId, Guid userId, UserRole role) => access.VisibleClients(tenantId, userId, role);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        var client = await Visible(tenant, user, role).AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.UserId, Name = x.User!.Name, x.User.Email, x.User.Phone, x.CoachId, x.Goals, x.Status, x.CheckInFrequency, x.CheckInDayOfWeek })
            .FirstOrDefaultAsync();
        return client is null ? NotFound() : Ok(client);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> Update(Guid id, UpdateClientRequest request)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        var client = await Visible(tenant, user, role).Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (client is null) return NotFound();
        if (request.Name is not null) client.User.Name = request.Name.Trim();
        if (request.Phone is not null) client.User.Phone = request.Phone.Trim();
        if (request.Goals is not null) client.Goals = request.Goals.Trim();
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("{id:guid}/overview")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> Overview(Guid id)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        var client = await Visible(tenant, user, role).AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, Email = x.User!.Email }).FirstOrDefaultAsync();
        if (client is null) return NotFound();
        var program = await db.Programs.AsNoTracking().Where(x => x.TenantId == tenant && x.ClientId == id && !x.IsTemplate).OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.Title, x.CoachId, x.CreatedAt }).FirstOrDefaultAsync();
        var subscription = await db.Subscriptions.AsNoTracking().Where(x => x.TenantId == tenant && x.ClientId == id).OrderByDescending(x => x.EndDate).Select(x => new { x.Id, x.Status, x.StartDate, x.EndDate, x.RemainingDays, x.SnapshotPrice, x.SnapshotDurationDays, PlanName = x.Plan.Name }).FirstOrDefaultAsync();
        var checkIn = await db.CheckIns.AsNoTracking().Where(x => x.TenantId == tenant && x.ClientId == id).OrderByDescending(x => x.Date).Select(x => new { x.Id, x.Date, x.Weight }).FirstOrDefaultAsync();
        var churn = await db.ChurnRiskLogs.AsNoTracking().Where(x => x.TenantId == tenant && x.ClientId == id).OrderByDescending(x => x.ComputedAt).Select(x => new { x.RiskScore, x.RiskLevel, x.ComputedAt }).FirstOrDefaultAsync();
        return Ok(new { client.Id, client.Email, CurrentProgram = program, Subscription = subscription, LatestCheckIn = checkIn, ChurnRisk = churn });
    }

    [HttpPut("{id:guid}/assign-coach")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> AssignCoach(Guid id, AssignCoachRequest request)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        if (role != UserRole.TenantOwner) return Forbid();
        var client = await Visible(tenant, user, role).FirstOrDefaultAsync(x => x.Id == id);
        if (client is null) return NotFound();
        var coach = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == request.CoachId && x.TenantId == tenant && x.IsActive && !x.IsDeleted && (x.Role == UserRole.TenantOwner || x.Role == UserRole.CoachStaff));
        if (coach is null) return BadRequest("Coach must be active and belong to this tenant.");
        client.CoachId = coach.Id; await db.SaveChangesAsync(); return NoContent();
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateClientStatusRequest request)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        if (!Enum.TryParse<ClientStatus>(request.Status, true, out var status) || !Enum.IsDefined(status)) return BadRequest("Status must be Active, Paused or Archived.");
        var client = await Visible(tenant, user, role).FirstOrDefaultAsync(x => x.Id == id);
        if (client is null) return NotFound();
        var old = client.Status; client.Status = status;
        db.SecurityAuditLogs.Add(Audit(user, tenant, "client.status.changed", $"client={id};from={old};to={status}"));
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("{id:guid}/health-info")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> HealthInfo(Guid id)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        var client = await Visible(tenant, user, role).Where(x => x.Id == id).Select(x => new { x.Id, x.HealthInfo }).FirstOrDefaultAsync();
        if (client is null) return NotFound();
        db.SecurityAuditLogs.Add(Audit(user, tenant, "client.health-info.read", $"client={id}")); await db.SaveChangesAsync();
        return Ok(new { client.Id, client.HealthInfo });
    }

    [HttpGet("{id:guid}/notes")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> Notes(Guid id)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        if (!await Visible(tenant, user, role).AnyAsync(x => x.Id == id)) return NotFound();
        var notes = await db.Set<ClientNote>().AsNoTracking().Where(x => x.TenantId == tenant && x.ClientId == id).OrderByDescending(x => x.CreatedAt).ToListAsync();
        return Ok(notes);
    }

    [HttpPost("{id:guid}/notes")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> AddNote(Guid id, CreateClientNoteRequest request)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Content)) return BadRequest("Note content is required.");
        if (!await Visible(tenant, user, role).AnyAsync(x => x.Id == id)) return NotFound();
        var note = new ClientNote { Id = Guid.NewGuid(), TenantId = tenant, ClientId = id, AuthorUserId = user, Content = request.Content.Trim() };
        db.Add(note); await db.SaveChangesAsync(); return CreatedAtAction(nameof(Notes), new { id }, note);
    }

    [HttpDelete("{id:guid}/notes/{noteId:guid}")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> DeleteNote(Guid id, Guid noteId)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        if (!await Visible(tenant, user, role).AnyAsync(x => x.Id == id)) return NotFound();
        var note = await db.Set<ClientNote>().FirstOrDefaultAsync(x => x.Id == noteId && x.ClientId == id && x.TenantId == tenant);
        if (note is null) return NotFound();
        if (role != UserRole.TenantOwner && note.AuthorUserId != user) return Forbid();
        db.Remove(note); await db.SaveChangesAsync(); return NoContent();
    }

    [HttpPut("{id:guid}/checkin-schedule")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> SetCheckInSchedule(Guid id, UpdateCheckInScheduleRequest request)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        if (!Enum.TryParse<CheckInFrequency>(request.Frequency, true, out var frequency) || !Enum.IsDefined(frequency) || !Enum.IsDefined(request.DayOfWeek)) return BadRequest("Frequency must be Weekly, BiWeekly or Monthly and dayOfWeek must be valid.");
        var client = await Visible(tenant, user, role).FirstOrDefaultAsync(x => x.Id == id);
        if (client is null) return NotFound();
        client.CheckInFrequency = frequency; client.CheckInDayOfWeek = request.DayOfWeek; await db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("invitations")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> Invitations()
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        var query = db.ClientInvitations.IgnoreQueryFilters().Where(x => x.TenantId == tenant);
        if (role == UserRole.CoachStaff) query = query.Where(x => x.CoachId == user);
        else if (role != UserRole.TenantOwner) return Forbid();
        var now = DateTime.UtcNow;
        var items = await query.OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.Email, x.CoachId, x.CreatedAt, x.ExpiresAt, x.AcceptedAt, Status = x.AcceptedAt != null ? "Accepted" : x.ExpiresAt <= now ? "Expired" : "Pending" }).ToListAsync();
        return Ok(items);
    }

    [HttpPost("invitations/{id:guid}/resend")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> ResendInvitation(Guid id)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        var query = db.ClientInvitations.IgnoreQueryFilters().Where(x => x.TenantId == tenant && x.Id == id);
        if (role == UserRole.CoachStaff) query = query.Where(x => x.CoachId == user);
        else if (role != UserRole.TenantOwner) return Forbid();
        var invitation = await query.FirstOrDefaultAsync(); if (invitation is null) return NotFound();
        if (invitation.AcceptedAt.HasValue) return Conflict("Accepted invitations cannot be resent.");
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        invitation.TokenHash = Hash(token); invitation.CreatedAt = DateTime.UtcNow; invitation.ExpiresAt = DateTime.UtcNow.AddHours(24);
        await db.SaveChangesAsync();
        var tenantLanguage = await db.Tenants.Where(x => x.Id == tenant).Select(x => x.DefaultLanguage).FirstOrDefaultAsync() ?? "en";
        await emailSender.SendTemplateAsync(invitation.Email, EmailTemplateType.ClientInvitation, tenantLanguage,
            new Dictionary<string, string> { ["token"] = token, ["name"] = invitation.InvitedName ?? string.Empty }, HttpContext.RequestAborted);
        return Ok(new { invitation.Id, invitation.Email, invitation.ExpiresAt, InvitationToken = environment.IsDevelopment() ? token : null });
    }

    [HttpDelete("invitations/{id:guid}")]
    [Authorize(Policy = "CoachOnly")]
    public async Task<IActionResult> DeleteInvitation(Guid id)
    {
        if (!TryScope(out var tenant, out var user, out var role)) return Unauthorized();
        var query = db.ClientInvitations.IgnoreQueryFilters().Where(x => x.TenantId == tenant && x.Id == id);
        if (role == UserRole.CoachStaff) query = query.Where(x => x.CoachId == user);
        else if (role != UserRole.TenantOwner) return Forbid();
        var invitation = await query.FirstOrDefaultAsync(); if (invitation is null) return NotFound();
        if (invitation.AcceptedAt.HasValue || invitation.ExpiresAt <= DateTime.UtcNow) return Conflict("Only pending invitations can be deleted.");
        db.Remove(invitation); await db.SaveChangesAsync(); return NoContent();
    }

    private SecurityAuditLog Audit(Guid userId, Guid tenantId, string type, string identifier) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId, EventType = type, Identifier = identifier,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString(), CreatedAt = DateTime.UtcNow
    };
}
