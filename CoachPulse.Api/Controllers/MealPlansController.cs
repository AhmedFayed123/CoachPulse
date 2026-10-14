using System.Security.Claims;
using System.Security.Cryptography;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController,Route("api/meal-plans"),Authorize(Policy="CoachOnly")]
public sealed class MealPlansController(CoachPulseDbContext db,IClientAccessService access):ControllerBase
{
 private Guid? Tenant=>Guid.TryParse(User.FindFirstValue("tenantId"),out var x)?x:null;private Guid? UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var x)?x:null;private bool Owner=>User.IsInRole("Owner");
 [HttpGet]
 public async Task<IActionResult> List(CancellationToken ct){if(Tenant is not{}t)return Unauthorized();var plans=await db.MealPlans.AsNoTracking().Where(x=>x.TenantId==t&&(Owner||x.IsTemplate||(x.ClientId!=null&&x.Client!.CoachId==UserId))).ToListAsync(ct);return Ok(plans.Select(View));}
 [HttpPost]
 public async Task<IActionResult> Create(MealPlanCreate req,CancellationToken ct){if(Tenant is not{}t||UserId is not{}u)return Unauthorized();if(string.IsNullOrWhiteSpace(req.Title)||req.Title.Length>200)return UnprocessableEntity();if(req.ClientId.HasValue){if(!Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role),out var role)||!await access.VisibleClients(t,u,role).AnyAsync(x=>x.Id==req.ClientId.Value,ct))return NotFound();}var p=new MealPlan{Id=Guid.NewGuid(),TenantId=t,ClientId=req.ClientId,Title=req.Title.Trim(),Meals="[]",IsTemplate=req.IsTemplate,Status=req.IsTemplate?"Template":"Draft",RowVersion=RandomNumberGenerator.GetBytes(8)};db.MealPlans.Add(p);await db.SaveChangesAsync(ct);return CreatedAtAction(nameof(Get),new{id=p.Id},await Full(p.Id,ct));}
 [HttpGet("{id:guid}")]
 public async Task<IActionResult> Get(Guid id,CancellationToken ct){var p=await Accessible().FirstOrDefaultAsync(x=>x.Id==id,ct);return p is null?NotFound():Ok(await Full(id,ct));}
 [HttpPut("{id:guid}")]
 public async Task<IActionResult> Update(Guid id,MealPlanUpdate req,CancellationToken ct){if(req.RowVersion is not{Length:>0})return UnprocessableEntity("rowVersion is required.");var p=await Accessible().FirstOrDefaultAsync(x=>x.Id==id,ct);if(p is null)return NotFound();if(p.Status=="Archived"||p.Status=="Active")return Conflict("Only a draft meal plan can be edited.");if(!p.RowVersion.SequenceEqual(req.RowVersion))return Conflict("Meal plan was changed by another request.");if(string.IsNullOrWhiteSpace(req.Title)||req.Title.Length>200)return UnprocessableEntity();p.Title=req.Title.Trim();p.RowVersion=RandomNumberGenerator.GetBytes(8);try{await db.SaveChangesAsync(ct);}catch(DbUpdateConcurrencyException){return Conflict();}return Ok(await Full(id,ct));}
 [HttpDelete("{id:guid}")]
 public async Task<IActionResult> Delete(Guid id,CancellationToken ct){var p=await Accessible().FirstOrDefaultAsync(x=>x.Id==id,ct);if(p is null)return NotFound();if(p.Status=="Active"&&p.ClientId.HasValue)return Conflict("Archive the assigned meal plan first.");p.IsDeleted=true;p.Status="Archived";Audit("meal-plan.deleted",p.Id);await db.SaveChangesAsync(ct);return NoContent();}
 [HttpPost("{id:guid}/meals")]
 public async Task<IActionResult> AddMeal(Guid id,MealRequest req,CancellationToken ct){var p=await Mutable(id,ct);if(p is null)return NotFound();if(!Valid(req))return UnprocessableEntity();var m=new Meal{Id=Guid.NewGuid(),MealPlanId=id,Name=req.Name.Trim(),Time=req.Time,Calories=req.Calories,Protein=req.Protein,Carbs=req.Carbs,Fat=req.Fat};db.Meals.Add(m);Bump(p);await db.SaveChangesAsync(ct);return StatusCode(201,new{meal=m,macroWarning=Warning(m)});}
 [HttpPut("{id:guid}/meals/{mealId:guid}")]
 public async Task<IActionResult> UpdateMeal(Guid id,Guid mealId,MealRequest req,CancellationToken ct){var p=await Mutable(id,ct);if(p is null)return NotFound();var m=await db.Meals.FirstOrDefaultAsync(x=>x.Id==mealId&&x.MealPlanId==id,ct);if(m is null)return NotFound();if(!Valid(req))return UnprocessableEntity();m.Name=req.Name.Trim();m.Time=req.Time;m.Calories=req.Calories;m.Protein=req.Protein;m.Carbs=req.Carbs;m.Fat=req.Fat;Bump(p);await db.SaveChangesAsync(ct);return Ok(new{meal=m,macroWarning=Warning(m)});}
 [HttpDelete("{id:guid}/meals/{mealId:guid}")]
 public async Task<IActionResult> DeleteMeal(Guid id,Guid mealId,CancellationToken ct){var p=await Mutable(id,ct);if(p is null)return NotFound();var m=await db.Meals.FirstOrDefaultAsync(x=>x.Id==mealId&&x.MealPlanId==id,ct);if(m is null)return NotFound();db.Meals.Remove(m);Bump(p);await db.SaveChangesAsync(ct);return NoContent();}
 [HttpPut("{id:guid}/assign")]
 public async Task<IActionResult> Assign(Guid id,MealPlanAssign req,CancellationToken ct){if(Tenant is not{}t||UserId is not{}u||!Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role),out var role))return Unauthorized();var p=await Accessible().FirstOrDefaultAsync(x=>x.Id==id,ct);if(p is null)return NotFound();if(p.IsTemplate)return Conflict("Create a copy from the template before assigning.");var c=await access.VisibleClients(t,u,role).FirstOrDefaultAsync(x=>x.Id==req.ClientId,ct);if(c is null)return NotFound();await using var tx=db.Database.IsRelational()?await db.Database.BeginTransactionAsync(ct):null;var current=await db.MealPlans.FirstOrDefaultAsync(x=>x.TenantId==t&&x.ClientId==c.Id&&x.Status=="Active"&&!x.IsTemplate&&x.Id!=id,ct);if(current is not null){current.Status="Archived";Bump(current);}p.ClientId=c.Id;p.Status="Active";Bump(p);Audit("meal-plan.assigned",p.Id,$"client={c.Id}");try{await db.SaveChangesAsync(ct);if(tx is not null)await tx.CommitAsync(ct);}catch(DbUpdateException){return Conflict("The client already has an active meal plan.");}return NoContent();}
 private IQueryable<MealPlan> Accessible()=>db.MealPlans.Where(x=>Owner||x.IsTemplate||(x.ClientId!=null&&x.Client!.CoachId==UserId));
 private async Task<MealPlan?> Mutable(Guid id,CancellationToken ct){var p=await Accessible().FirstOrDefaultAsync(x=>x.Id==id,ct);return p is not null&&p.Status!="Archived"?p:null;}
 private async Task<object?> Full(Guid id,CancellationToken ct){var p=await db.MealPlans.AsNoTracking().Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id,ct);return p is null?null:new{p.Id,p.Title,p.ClientId,p.IsTemplate,p.Status,rowVersion=p.RowVersion,meals=p.Items.Select(m=>new{m.Id,m.Name,m.Time,m.Calories,m.Protein,m.Carbs,m.Fat,macroWarning=Warning(m)}),totals=new{calories=p.Items.Sum(m=>m.Calories),protein=p.Items.Sum(m=>m.Protein),carbs=p.Items.Sum(m=>m.Carbs),fat=p.Items.Sum(m=>m.Fat)},macroWarning=Warning(p.Items.Sum(m=>m.Calories),p.Items.Sum(m=>m.Protein),p.Items.Sum(m=>m.Carbs),p.Items.Sum(m=>m.Fat))};}
 private static object View(MealPlan p)=>new{p.Id,p.Title,p.ClientId,p.IsTemplate,p.Status,p.RowVersion};
 private static bool Valid(MealRequest r)=>!string.IsNullOrWhiteSpace(r.Name)&&r.Name.Length<=200&&r.Calories is >=0 and <=10000&&r.Protein is >=0 and <=1000&&r.Carbs is >=0 and <=1500&&r.Fat is >=0 and <=500;
 private static bool Warning(Meal m)=>Warning(m.Calories,m.Protein,m.Carbs,m.Fat);
 private static bool Warning(decimal calories,decimal protein,decimal carbs,decimal fat){var estimate=4*protein+4*carbs+9*fat;return estimate>0&&(calories==0||Math.Abs(calories-estimate)/calories>.15m);}
 private void Bump(MealPlan p)=>p.RowVersion=RandomNumberGenerator.GetBytes(8);
 private void Audit(string e,Guid id,string? more=null)=>db.SecurityAuditLogs.Add(new SecurityAuditLog{Id=Guid.NewGuid(),TenantId=Tenant,UserId=UserId,EventType=e,Identifier=$"mealPlan={id};{more}"});
}
[ApiController,Route("api"),Authorize(Policy="ClientOnly")]
public sealed class MyMealPlanController(CoachPulseDbContext db):ControllerBase
{
 [HttpGet("my/meal-plan")]
 public async Task<IActionResult> Mine(CancellationToken ct){if(!Guid.TryParse(User.FindFirstValue("tenantId"),out var t)||!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var u))return Unauthorized();var c=await db.Clients.FirstOrDefaultAsync(x=>x.TenantId==t&&x.UserId==u,ct);if(c is null)return NotFound();var p=await db.MealPlans.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.ClientId==c.Id&&x.Status=="Active"&&!x.IsTemplate,ct);return p is null?Ok(null):Ok(new{p.Id,p.Title,p.Status,meals=p.Items.Select(m=>new{m.Id,m.Name,m.Time,m.Calories,m.Protein,m.Carbs,m.Fat}),totals=new{calories=p.Items.Sum(m=>m.Calories),protein=p.Items.Sum(m=>m.Protein),carbs=p.Items.Sum(m=>m.Carbs),fat=p.Items.Sum(m=>m.Fat)}});}
}
[ApiController,Route("api/clients/{id:guid}/meal-plan"),Authorize(Policy="CoachOnly")]
public sealed class ClientMealPlanController(CoachPulseDbContext db,IClientAccessService access):ControllerBase
{
 [HttpGet] public async Task<IActionResult> Get(Guid id,CancellationToken ct){if(!Guid.TryParse(User.FindFirstValue("tenantId"),out var t)||!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var u)||!Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role),out var role))return Unauthorized();var c=await access.VisibleClients(t,u,role).FirstOrDefaultAsync(x=>x.Id==id,ct);if(c is null)return NotFound();var p=await db.MealPlans.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.ClientId==id&&x.Status=="Active",ct);return p is null?Ok(null):Ok(new{p.Id,p.Title,meals=p.Items});}
}
[ApiController,Route("api/my/meal-logs"),Authorize(Policy="ClientOnly")]
public sealed class MyMealLogsController(CoachPulseDbContext db):ControllerBase
{
 [HttpPost]
 public async Task<IActionResult> Log(MyMealLogRequest request,CancellationToken ct)
 {
  if(!Guid.TryParse(User.FindFirstValue("tenantId"),out var tenant)||!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var user))return Unauthorized();
  var client=await db.Clients.FirstOrDefaultAsync(x=>x.TenantId==tenant&&x.UserId==user,ct);if(client is null)return NotFound();
  if(request.LoggedOn>DateOnly.FromDateTime(DateTime.UtcNow))return UnprocessableEntity("LoggedOn cannot be in the future.");
  var meal=await db.Meals.FirstOrDefaultAsync(x=>x.Id==request.MealId&&db.MealPlans.Any(p=>p.Id==x.MealPlanId&&p.TenantId==tenant&&p.ClientId==client.Id&&p.Status=="Active"&&!p.IsTemplate),ct);if(meal is null)return UnprocessableEntity("Meal must belong to the client's current meal plan.");
  var row=new CoachPulse.Domain.Entities.MealLog{Id=Guid.NewGuid(),TenantId=tenant,ClientId=client.Id,MealId=meal.Id,LoggedOn=request.LoggedOn,CreatedAt=DateTime.UtcNow};db.MealLogs.Add(row);
  try{await db.SaveChangesAsync(ct);}catch(DbUpdateException){return Conflict("This meal has already been logged for that date.");}
  return StatusCode(201,new{row.Id,row.MealId,row.LoggedOn,row.CreatedAt});
 }
}
public sealed record MyMealLogRequest(Guid MealId,DateOnly LoggedOn);
public sealed record MealPlanCreate(string Title,bool IsTemplate=false,Guid? ClientId=null);
public sealed record MealPlanUpdate(string Title,byte[]? RowVersion);
public sealed record MealPlanAssign(Guid ClientId);
public sealed record MealRequest(string Name,TimeOnly? Time,decimal Calories,decimal Protein,decimal Carbs,decimal Fat);

