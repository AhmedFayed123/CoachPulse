using System.Security.Claims;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController, Authorize]
public sealed class WorkoutLogsController(CoachPulseDbContext db,IClientAccessService access):ControllerBase
{
 private Guid? Tenant=>Guid.TryParse(User.FindFirstValue("tenantId"),out var x)?x:null; private Guid? UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var x)?x:null;
 [HttpPost("/api/my/workout-logs"),Authorize(Policy="ClientOnly")]
 public async Task<IActionResult> Create(WorkoutLogRequest r,CancellationToken ct)
 {
  if(Tenant is not{} t||UserId is not{} u)return Unauthorized();var c=await db.Clients.FirstOrDefaultAsync(x=>x.TenantId==t&&x.UserId==u,ct);if(c is null)return NotFound();
  if(r.SetNumber is <1 or >20||r.Reps<1||r.WeightKg is <0 or >1000)return UnprocessableEntity();
  var pe=await db.ProgramExercises.Include(x=>x.Program).FirstOrDefaultAsync(x=>x.Id==r.ProgramExerciseId&&x.Program.TenantId==t&&x.Program.ClientId==c.Id&&x.Program.Status=="Active"&&!x.Program.IsTemplate,ct);if(pe is null)return UnprocessableEntity("The exercise must belong to your active program.");
  var tenant=await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==t,ct);TimeZoneInfo zone;try{zone=TimeZoneInfo.FindSystemTimeZoneById(tenant?.TimeZone??"Africa/Cairo");}catch{zone=TimeZoneInfo.Utc;}
  var performedUtc=DateTime.SpecifyKind(r.PerformedAt,DateTimeKind.Utc);var localDay=DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(performedUtc,zone));var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,zone));
  if(localDay>today||localDay<today.AddDays(-30))return UnprocessableEntity("Workout date must be within the last 30 local calendar days.");
  if(await db.WorkoutLogs.AnyAsync(x=>x.TenantId==t&&x.ClientId==c.Id&&x.ProgramExerciseId==r.ProgramExerciseId&&x.PerformedOn==localDay&&x.SetNumber==r.SetNumber,ct))return Conflict("That set already exists; update the existing set instead.");
  var log=new WorkoutLog{Id=Guid.NewGuid(),TenantId=t,ClientId=c.Id,ProgramExerciseId=pe.Id,SetNumber=r.SetNumber,Reps=r.Reps,WeightKg=r.WeightKg,PerformedAt=performedUtc,PerformedOn=localDay,CreatedAt=DateTime.UtcNow};db.WorkoutLogs.Add(log);await db.SaveChangesAsync(ct);return StatusCode(201,log);
 }
 [HttpGet("/api/my/workout-logs"),Authorize(Policy="ClientOnly")]
 public async Task<IActionResult> Mine([FromQuery]DateTime? from,[FromQuery]DateTime? to,[FromQuery]int page=1,[FromQuery]int pageSize=50,CancellationToken ct=default){if(Tenant is not{}t||UserId is not{}u)return Unauthorized();var c=await db.Clients.FirstOrDefaultAsync(x=>x.TenantId==t&&x.UserId==u,ct);if(c is null)return NotFound();return await List(c.Id,from,to,page,pageSize,ct);}
 [HttpPut("/api/my/workout-logs/{id:guid}"),Authorize(Policy="ClientOnly")]
 public async Task<IActionResult> Update(Guid id,WorkoutLogUpdate r,CancellationToken ct){var l=await OwnLog(id,ct);if(l is null)return NotFound();if(DateTime.UtcNow-l.CreatedAt>TimeSpan.FromHours(48))return Conflict("Workout logs can only be edited within 48 hours.");if(r.Reps<1||r.WeightKg is <0 or >1000)return UnprocessableEntity();l.Reps=r.Reps;l.WeightKg=r.WeightKg;await db.SaveChangesAsync(ct);return Ok(l);}
 [HttpDelete("/api/my/workout-logs/{id:guid}"),Authorize(Policy="ClientOnly")]
 public async Task<IActionResult> Delete(Guid id,CancellationToken ct){var l=await OwnLog(id,ct);if(l is null)return NotFound();if(DateTime.UtcNow-l.CreatedAt>TimeSpan.FromHours(48))return Conflict();l.IsDeleted=true;await db.SaveChangesAsync(ct);return NoContent();}
 [HttpGet("/api/clients/{id:guid}/workout-logs"),Authorize(Policy="CoachOnly")]
 public async Task<IActionResult> ClientLogs(Guid id,[FromQuery]DateTime? from,[FromQuery]DateTime? to,[FromQuery]int page=1,[FromQuery]int pageSize=50,CancellationToken ct=default){if(Tenant is not{}t||UserId is not{}u||!Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role),out var role))return Unauthorized();var c=await access.VisibleClients(t,u,role).FirstOrDefaultAsync(x=>x.Id==id,ct);return c is null?NotFound():await List(id,from,to,page,pageSize,ct);}
 [HttpGet("/api/clients/{id:guid}/adherence"),Authorize(Policy="CoachOnly")]
 public async Task<IActionResult> Adherence(Guid id,[FromQuery]string period="week",[FromQuery]DateOnly? date=null,CancellationToken ct=default){if(Tenant is not{}t||UserId is not{}u||!Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role),out var role))return Unauthorized();var c=await access.VisibleClients(t,u,role).FirstOrDefaultAsync(x=>x.Id==id,ct);if(c is null)return NotFound();if(period is not("week" or "month"))return UnprocessableEntity();var selected=date??DateOnly.FromDateTime(DateTime.UtcNow);var end=selected.AddDays(1).ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc).AddTicks(-1);var start=period=="week"?selected.AddDays(-6).ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc):new DateTime(selected.Year,selected.Month,1,0,0,0,DateTimeKind.Utc);var result=await CoachPulse.Api.Services.WorkoutAdherenceCalculator.CalculateAsync(db,t,id,start,end,ct);return Ok(new{period,planned=result.Rate is null?(int?)null:result.Planned,completed=result.Rate is null?(int?)null:result.Completed,rate=result.Rate});}
 private async Task<IActionResult> List(Guid client,DateTime? from,DateTime? to,int page,int pageSize,CancellationToken ct){if((to??DateTime.UtcNow)-(from??DateTime.UtcNow.AddDays(-366))>TimeSpan.FromDays(366))return UnprocessableEntity();if(page<1||pageSize<1||pageSize>100)return UnprocessableEntity();var q=db.WorkoutLogs.AsNoTracking().Where(x=>x.ClientId==client);if(from.HasValue)q=q.Where(x=>x.PerformedAt>=from);if(to.HasValue)q=q.Where(x=>x.PerformedAt<=to);var total=await q.CountAsync(ct);var rows=await q.OrderByDescending(x=>x.PerformedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);return Ok(new{total,page,pageSize,items=rows});}
 private async Task<WorkoutLog?> OwnLog(Guid id,CancellationToken ct){if(Tenant is not{}t||UserId is not{}u)return null;var c=await db.Clients.FirstOrDefaultAsync(x=>x.TenantId==t&&x.UserId==u,ct);return c is null?null:await db.WorkoutLogs.FirstOrDefaultAsync(x=>x.Id==id&&x.TenantId==t&&x.ClientId==c.Id,ct);}
}
public sealed record WorkoutLogRequest(Guid ProgramExerciseId,int SetNumber,int Reps,decimal WeightKg,DateTime PerformedAt);
public sealed record WorkoutLogUpdate(int Reps,decimal WeightKg);
