using System.Security.Claims;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController, Route("api/my"), Authorize(Policy = "ClientOnly")]
public sealed class MyExperienceController(CoachPulseDbContext db) : ControllerBase
{
    private bool Scope(out Guid tenant, out Guid user)
    { tenant = user = Guid.Empty; return Guid.TryParse(User.FindFirstValue("tenantId"), out tenant) && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out user); }
    private Task<Client?> Client(Guid tenant, Guid user, CancellationToken ct) => db.Clients.FirstOrDefaultAsync(x => x.TenantId == tenant && x.UserId == user, ct);
    private static object Profile(User u) => new { u.Id, u.Name, u.Phone, u.Language, u.AvatarFileId };

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct) { if (!Scope(out var t, out var u)) return Unauthorized(); var user = await db.Users.FirstOrDefaultAsync(x => x.Id == u && x.TenantId == t, ct); return user is null ? NotFound() : Ok(Profile(user)); }
    [HttpPut("profile")]
    public async Task<IActionResult> PutProfile(ProfileUpdate req, CancellationToken ct)
    {
        if (!Scope(out var t, out var u)) return Unauthorized(); var user = await db.Users.FirstOrDefaultAsync(x => x.Id == u && x.TenantId == t, ct); if (user is null) return NotFound();
        if (req.Name is { Length: > 100 } || req.Phone is { Length: > 30 } || req.Language is { Length: > 10 }) return UnprocessableEntity();
        if (req.Name is not null) user.Name = req.Name.Trim(); if (req.Phone is not null) user.Phone = req.Phone.Trim(); if (req.Language is not null) user.Language = req.Language.Trim();
        if (req.AvatarFileId.HasValue) { var f = await db.StoredFiles.FirstOrDefaultAsync(x => x.Id == req.AvatarFileId && x.TenantId == t && x.OwnerUserId == u && x.Purpose == "avatar" && !x.IsDeleted, ct); if (f is null) return UnprocessableEntity("avatarFileId must identify your avatar file."); user.AvatarFileId = f.Id; }
        await db.SaveChangesAsync(ct); return Ok(Profile(user));
    }
    [HttpPost("onboarding")]
    public async Task<IActionResult> Onboarding(OnboardingRequest req, CancellationToken ct)
    {
        if (!Scope(out var t, out var u)) return Unauthorized(); if (req.Weight is < 20 or > 500) return UnprocessableEntity("Weight must be between 20 and 500 kg.");
        var c = await Client(t,u,ct); if (c is null) return NotFound(); if (c.OnboardingCompleted) return Conflict("Onboarding is already complete.");
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null; c.Goals = req.Goals?.Trim(); c.HealthInfo = req.HealthInfo?.Trim();
        if (req.Weight.HasValue) db.CheckIns.Add(new CheckIn { Id=Guid.NewGuid(),TenantId=t,ClientId=c.Id,Date=DateTime.UtcNow,Weight=req.Weight.Value,Measurements=req.Measurements });
        c.OnboardingCompleted = true; await db.SaveChangesAsync(ct); if (tx is not null) await tx.CommitAsync(ct); return StatusCode(201,new { completed=true });
    }
    [HttpGet("health-info")]
    public async Task<IActionResult> GetHealth(CancellationToken ct) { if (!Scope(out var t,out var u)) return Unauthorized(); var c=await Client(t,u,ct); return c is null?NotFound():Ok(new { c.HealthInfo }); }
    [HttpPut("health-info")]
    public async Task<IActionResult> PutHealth(HealthInfoUpdate req,CancellationToken ct) { if (!Scope(out var t,out var u)) return Unauthorized(); if(req.HealthInfo?.Length>4000)return UnprocessableEntity(); var c=await Client(t,u,ct);if(c is null)return NotFound();c.HealthInfo=req.HealthInfo?.Trim();db.SecurityAuditLogs.Add(new SecurityAuditLog{Id=Guid.NewGuid(),TenantId=t,UserId=u,EventType="client.health-info.updated",Identifier=$"client={c.Id}"});await db.SaveChangesAsync(ct);return NoContent(); }
    [HttpGet("coach")]
    public async Task<IActionResult> Coach(CancellationToken ct) { if(!Scope(out var t,out var u))return Unauthorized();var c=await Client(t,u,ct);if(c is null)return NotFound();var coach=c.CoachId is null?null:await db.Users.AsNoTracking().Where(x=>x.Id==c.CoachId&&x.TenantId==t).Select(x=>new{x.Id,x.Name,x.AvatarFileId,Specialty=(string?)null}).FirstOrDefaultAsync(ct);return Ok(coach); }
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        if(!Scope(out var t,out var u))return Unauthorized();var c=await Client(t,u,ct);if(c is null)return NotFound();var now=DateTime.UtcNow;
        var program=await db.Programs.AsNoTracking().Where(x=>x.TenantId==t&&x.ClientId==c.Id&&x.Status=="Active"&&!x.IsTemplate).OrderByDescending(x=>x.StartDate).Select(x=>new{x.Id,x.Title,x.StartDate,x.DurationDays}).FirstOrDefaultAsync(ct);
        var sub=await db.Subscriptions.AsNoTracking().Where(x=>x.TenantId==t&&x.ClientId==c.Id).OrderByDescending(x=>x.EndDate).Select(x=>new{x.Status,x.EndDate}).FirstOrDefaultAsync(ct);
        var unread=await db.Messages.CountAsync(x=>x.TenantId==t&&x.ReceiverId==u&&!x.IsRead&&!x.IsDeleted,ct);
        var tenant=await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==t,ct);TimeZoneInfo zone;try{zone=TimeZoneInfo.FindSystemTimeZoneById(tenant?.TimeZone??"Africa/Cairo");}catch{zone=TimeZoneInfo.Utc;}var local=TimeZoneInfo.ConvertTimeFromUtc(now,zone);object? todayWorkout=null;
        if(program is not null){var startUtc=DateTime.SpecifyKind(program.StartDate??now,DateTimeKind.Utc);var startLocal=TimeZoneInfo.ConvertTimeFromUtc(startUtc,zone).Date;var day=(local.Date-startLocal).Days+1;if(day>0&&day<=program.DurationDays){var exercises=await(from pe in db.ProgramExercises.AsNoTracking() join e in db.Exercises.AsNoTracking() on pe.ExerciseId equals e.Id where pe.ProgramId==program.Id&&pe.Day==day orderby pe.SortOrder select new{pe.Id,pe.ExerciseId,ExerciseName=e.Name,pe.Sets,pe.Reps,pe.RestSeconds}).ToListAsync(ct);todayWorkout=new{program.Id,dayNumber=day,restDay=exercises.Count==0,exercises};}}
        DateTime? next=null;if(c.CheckInFrequency is not null&&c.CheckInDayOfWeek is not null){var last=await db.CheckIns.AsNoTracking().Where(x=>x.TenantId==t&&x.ClientId==c.Id).OrderByDescending(x=>x.Date).Select(x=>(DateTime?)x.Date).FirstOrDefaultAsync(ct);var candidate=local.Date.AddDays(((int)c.CheckInDayOfWeek.Value-(int)local.DayOfWeek+7)%7);if(candidate<=local.Date)candidate=candidate.AddDays(7);if(c.CheckInFrequency==CheckInFrequency.BiWeekly&&last is not null){var min=TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(last.Value,DateTimeKind.Utc),zone).Date.AddDays(14);while(candidate<min)candidate=candidate.AddDays(7);}if(c.CheckInFrequency==CheckInFrequency.Monthly&&last is not null){var min=TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(last.Value,DateTimeKind.Utc),zone).Date.AddMonths(1);while(candidate<min)candidate=candidate.AddDays(7);}next=candidate;}
        return Ok(new{todayWorkout,nextCheckIn=next,subscription=sub,unreadMessages=unread,program=program is null?null:new{program.Id,program.Title,program.StartDate,program.DurationDays}});
    }
}
public sealed record ProfileUpdate(string? Name,string? Phone,string? Language,Guid? AvatarFileId);
public sealed record OnboardingRequest(string? Goals,string? HealthInfo,decimal? Weight,string? Measurements);
public sealed record HealthInfoUpdate(string? HealthInfo);
