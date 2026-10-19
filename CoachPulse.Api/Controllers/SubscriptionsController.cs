using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoachPulse.Api.Hubs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController, Authorize]
public sealed class SubscriptionsController(CoachPulseDbContext db, ISubscriptionActivationService activation,
    ISubscriptionStateMachine states, IHubContext<NotificationsHub> hub) : ControllerBase
{
    [HttpGet("api/my/subscription"), Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> Current(CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out var user, out _)) return Unauthorized();
        var clientId = await db.Clients.Where(x => x.TenantId == tenant && x.UserId == user).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (clientId is null) return NotFound();
        var sub = await db.Subscriptions.AsNoTracking().Include(x => x.Plan).Where(x => x.ClientId == clientId && x.TenantId == tenant && x.Status == SubscriptionStatus.Active).OrderByDescending(x => x.EndDate).Select(x => View(x)).FirstOrDefaultAsync(ct);
        return sub is null ? NotFound() : Ok(sub);
    }

    [HttpGet("api/my/subscriptions"), Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out var user, out _)) return Unauthorized();
        var clientId = await db.Clients.Where(x => x.TenantId == tenant && x.UserId == user).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (clientId is null) return NotFound();
        return Ok(await db.Subscriptions.AsNoTracking().Include(x => x.Plan).Where(x => x.ClientId == clientId && x.TenantId == tenant).OrderByDescending(x => x.StartDate).Select(x => View(x)).ToListAsync(ct));
    }

    [HttpPost("api/my/subscriptions"), Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest req, CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out var user, out _)) return Unauthorized();
        var idempotency = Request.Headers["Idempotency-Key"].ToString(); if (string.IsNullOrWhiteSpace(idempotency)) return BadRequest(new { message="Idempotency-Key is required." });
        var existingKey = await ReadIdempotency(idempotency, user, tenant, "subscription.create", req, ct);
        if (existingKey.Result is not null) return existingKey.Result;
        var client = await db.Clients.FirstOrDefaultAsync(x => x.TenantId == tenant && x.UserId == user, ct); if (client is null) return NotFound();
        var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == req.PlanId && x.TenantId == tenant && x.IsActive, ct); if (plan is null) return NotFound();
        if (await db.Subscriptions.AnyAsync(x => x.TenantId == tenant && x.ClientId == client.Id && x.PlanId == plan.Id && (x.Status == SubscriptionStatus.Pending || x.Status == SubscriptionStatus.Active), ct)) return Conflict("A pending or active subscription for this plan already exists.");
        var sub = new Subscription { Id=Guid.NewGuid(), TenantId=tenant, ClientId=client.Id, PlanId=plan.Id, Status=SubscriptionStatus.Pending, StartDate=DateTime.UtcNow, EndDate=DateTime.UtcNow, SnapshotPrice=plan.Price, SnapshotDurationDays=plan.DurationDays };
        db.Subscriptions.Add(sub);
        var body = new { sub.Id, sub.PlanId, sub.Status, sub.SnapshotPrice, sub.SnapshotDurationDays };
        SaveIdempotency(idempotency,user,tenant,"subscription.create",req,201,body);
        await db.SaveChangesAsync(ct); return StatusCode(201, body);
    }

    [HttpPost("api/my/subscription/renew"), Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> Renew([FromBody] RenewSubscriptionRequest req, CancellationToken ct) => await Create(new CreateSubscriptionRequest(req.PlanId), ct);

    [HttpPut("api/my/subscription/cancel"), Authorize(Policy="ClientOnly")]
    public async Task<IActionResult> ClientCancel([FromBody] CancelSubscriptionRequest req, CancellationToken ct)
    {
        if (!TryIdentity(out var tenant,out var user,out _)) return Unauthorized();
        var sub=await db.Subscriptions.FirstOrDefaultAsync(x=>x.Id==req.SubscriptionId && x.TenantId==tenant && x.Client.UserId==user,ct); if(sub is null)return NotFound();
        try { states.Transition(sub,SubscriptionStatus.Cancelled,req.Reason); } catch(InvalidOperationException ex){return Conflict(ex.Message);}
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpGet("api/subscriptions"), Authorize(Policy="CoachOnly")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if(!TryIdentity(out var tenant,out var user,out var role))return Unauthorized();
        var q=db.Subscriptions.AsNoTracking().Include(x=>x.Client).Where(x=>x.TenantId==tenant);
        if(role==UserRole.CoachStaff) q=q.Where(x=>x.Client.CoachId==user);
        return Ok(await q.OrderByDescending(x=>x.StartDate).Select(x=>View(x)).ToListAsync(ct));
    }

    [HttpGet("api/subscriptions/{id:guid}"), Authorize(Policy="CoachOnly")]
    public async Task<IActionResult> Get(Guid id,CancellationToken ct)
    {
        if(!TryIdentity(out var tenant,out var user,out var role))return Unauthorized();
        var q=db.Subscriptions.AsNoTracking().Include(x=>x.Client).Where(x=>x.Id==id && x.TenantId==tenant);
        if(role==UserRole.CoachStaff)q=q.Where(x=>x.Client.CoachId==user);
        var value=await q.Select(x=>View(x)).FirstOrDefaultAsync(ct);return value is null?NotFound():Ok(value);
    }

    [HttpPost("api/subscriptions"), Authorize(Policy="OwnerOnly")]
    public async Task<IActionResult> ManualActivate([FromBody] ManualSubscriptionRequest req,CancellationToken ct)
    {
        if(!TryIdentity(out var tenant,out var user,out _))return Unauthorized();
        var client=await db.Clients.FirstOrDefaultAsync(x=>x.Id==req.ClientId && x.TenantId==tenant,ct);var plan=await db.Plans.FirstOrDefaultAsync(x=>x.Id==req.PlanId && x.TenantId==tenant && x.IsActive,ct);
        if(client is null||plan is null)return NotFound();
        if(await db.Subscriptions.AnyAsync(x=>x.TenantId==tenant&&x.ClientId==client.Id&&x.PlanId==plan.Id&&x.Status==SubscriptionStatus.Active,ct))return Conflict("An active subscription for this plan already exists.");
        var sub=new Subscription{Id=Guid.NewGuid(),TenantId=tenant,ClientId=client.Id,PlanId=plan.Id,Status=SubscriptionStatus.Pending,StartDate=DateTime.UtcNow,EndDate=DateTime.UtcNow,SnapshotPrice=plan.Price,SnapshotDurationDays=plan.DurationDays};db.Subscriptions.Add(sub);
        try{await activation.ActivateAsync(sub,SubscriptionActivationSource.OwnerManual,ct);}catch(InvalidOperationException ex){return Conflict(ex.Message);}
        Audit(user,tenant,"subscription.activated.manual",$"subscription={sub.Id};client={client.Id};plan={plan.Id}");await db.SaveChangesAsync(ct);
        await hub.Clients.User(client.UserId.ToString()).SendAsync("SubscriptionStatusChanged",new{sub.Id,status=sub.Status},ct);return Created($"/api/subscriptions/{sub.Id}",new{SubId=sub.Id,sub.Status,sub.StartDate,sub.EndDate});
    }

    [HttpPut("api/subscriptions/{id:guid}/extend"),Authorize(Policy="OwnerOnly")]
    public Task<IActionResult> Extend(Guid id,[FromBody] SubscriptionActionRequest req,CancellationToken ct)=>Act(id,req,"extend",ct);
    [HttpPut("api/subscriptions/{id:guid}/pause"),Authorize(Policy="OwnerOnly")]
    public Task<IActionResult> Pause(Guid id,[FromBody] SubscriptionActionRequest req,CancellationToken ct)=>Act(id,req,"pause",ct);
    [HttpPut("api/subscriptions/{id:guid}/resume"),Authorize(Policy="OwnerOnly")]
    public Task<IActionResult> Resume(Guid id,[FromBody] SubscriptionActionRequest req,CancellationToken ct)=>Act(id,req,"resume",ct);
    [HttpPut("api/subscriptions/{id:guid}/cancel"),Authorize(Policy="OwnerOnly")]
    public Task<IActionResult> Cancel(Guid id,[FromBody] SubscriptionActionRequest req,CancellationToken ct)=>Act(id,req,"cancel",ct);

    private async Task<IActionResult> Act(Guid id,SubscriptionActionRequest req,string action,CancellationToken ct)
    {
        if(!TryIdentity(out var tenant,out var user,out _))return Unauthorized();if(string.IsNullOrWhiteSpace(req.Reason))return UnprocessableEntity("Reason is required.");
        var s=await db.Subscriptions.Include(x=>x.Client).FirstOrDefaultAsync(x=>x.Id==id && x.TenantId==tenant,ct);if(s is null)return NotFound();
        try { switch(action){case "extend":var days=req.Days.GetValueOrDefault();if(days is <1 or >3650)return UnprocessableEntity();if(s.Status!=SubscriptionStatus.Active)return Conflict();s.EndDate=s.EndDate.AddDays(days);break;case "pause":states.Transition(s,SubscriptionStatus.Paused,req.Reason);break;case "resume":states.Transition(s,SubscriptionStatus.Active,req.Reason);break;default:states.Transition(s,SubscriptionStatus.Cancelled,req.Reason);break;} }
        catch(InvalidOperationException ex){return Conflict(ex.Message);}
        Audit(user,tenant,$"subscription.{action}",$"subscription={s.Id};days={req.Days};reason={req.Reason}");await db.SaveChangesAsync(ct);
        await hub.Clients.User(s.Client.UserId.ToString()).SendAsync("SubscriptionStatusChanged",new{s.Id,status=s.Status},ct);return Ok(new{s.Id,status=s.Status,s.StartDate,s.EndDate,s.RemainingDays});
    }

    private (IActionResult? Result, bool Replay) ReadReplay(IdempotencyKey? key, string hash) => (null,false);
    private async Task<(IActionResult? Result, bool Replay)> ReadIdempotency(string key,Guid user,Guid tenant,string endpoint,object body,CancellationToken ct)
    {
        var expired=await db.IdempotencyKeys.Where(x=>x.TenantId==tenant&&x.UserId==user&&x.Endpoint==endpoint&&x.Key==key&&x.CreatedAt<DateTime.UtcNow.AddHours(-24)).ToListAsync(ct);if(expired.Count>0){db.IdempotencyKeys.RemoveRange(expired);await db.SaveChangesAsync(ct);}
        var hash=Hash(JsonSerializer.Serialize(body,JsonSerializerOptions.Web));var row=await db.IdempotencyKeys.FirstOrDefaultAsync(x=>x.TenantId==tenant&&x.UserId==user&&x.Endpoint==endpoint&&x.Key==key&&x.CreatedAt>=DateTime.UtcNow.AddHours(-24),ct);
        if(row is null)return(null,false);if(row.RequestHash!=hash)return(UnprocessableEntity("Idempotency-Key was used with a different request."),true);
        var snapshot=JsonSerializer.Deserialize<Snapshot>(row.ResponseSnapshot,JsonSerializerOptions.Web)!;return(new ContentResult{Content=snapshot.Body,ContentType="application/json",StatusCode=snapshot.Status},true);
    }
    private void SaveIdempotency(string key,Guid user,Guid tenant,string endpoint,object request,int status,object body)=>db.IdempotencyKeys.Add(new IdempotencyKey{Id=Guid.NewGuid(),TenantId=tenant,UserId=user,Endpoint=endpoint,Key=key,RequestHash=Hash(JsonSerializer.Serialize(request,JsonSerializerOptions.Web)),ResponseSnapshot=JsonSerializer.Serialize(new Snapshot(status,JsonSerializer.Serialize(body,JsonSerializerOptions.Web)),JsonSerializerOptions.Web),CreatedAt=DateTime.UtcNow});
    private static string Hash(string s)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
    private static object View(Subscription x)=>new{x.Id,x.TenantId,x.ClientId,x.PlanId,PlanName=x.Plan.Name,Price=x.SnapshotPrice,DurationDays=x.SnapshotDurationDays,x.Status,x.StartDate,x.EndDate,x.RemainingDays};
    private bool TryIdentity(out Guid tenant,out Guid user,out UserRole role){tenant=Guid.Empty;user=Guid.Empty;role=default;return Guid.TryParse(User.FindFirst("tenantId")?.Value,out tenant)&&Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value,out user)&&Enum.TryParse(User.FindFirst(ClaimTypes.Role)?.Value,out role);}
    private void Audit(Guid user,Guid tenant,string type,string identifier)=>db.SecurityAuditLogs.Add(new(){Id=Guid.NewGuid(),UserId=user,TenantId=tenant,EventType=type,Identifier=identifier,CreatedAt=DateTime.UtcNow});
    private sealed record Snapshot(int Status,string Body);
}
public sealed record CreateSubscriptionRequest(Guid PlanId);
public sealed record RenewSubscriptionRequest(Guid PlanId);
public sealed record CancelSubscriptionRequest(Guid SubscriptionId,string? Reason);
public sealed record ManualSubscriptionRequest(Guid ClientId,Guid PlanId,string? Reason);
public sealed record SubscriptionActionRequest(string? Reason,int? Days);
