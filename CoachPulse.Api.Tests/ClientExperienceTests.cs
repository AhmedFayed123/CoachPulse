using System.Security.Claims;
using CoachPulse.Api.Controllers;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class ClientExperienceTests
{
    [Fact]
    public async Task OnboardingCanOnlyBeSubmittedOnce()
    {
        await using var f = await Fixture.Create(UserRole.Client); var c = f.AddClient(f.UserId); await f.Db.SaveChangesAsync();
        var first = await f.Self.Onboarding(new OnboardingRequest("Strength", "None", 70, null), default);
        var second = await f.Self.Onboarding(new OnboardingRequest("Other", null, 71, null), default);
        Assert.IsType<ObjectResult>(first); Assert.Equal(201, ((ObjectResult)first).StatusCode); Assert.IsType<ConflictObjectResult>(second);
    }

    [Fact]
    public async Task ProfileUpdateDoesNotExposeOrModifyRoleOrTenant()
    {
        await using var f = await Fixture.Create(UserRole.Client); var c=f.AddClient(f.UserId);await f.Db.SaveChangesAsync();
        var result=Assert.IsType<OkObjectResult>(await f.Self.PutProfile(new ProfileUpdate("New Name",null,"ar",null),default));
        Assert.Equal(UserRole.Client, f.Db.Users.Single(x=>x.Id==f.UserId).Role); Assert.Equal(f.TenantId,f.Db.Users.Single(x=>x.Id==f.UserId).TenantId); Assert.DoesNotContain("Role",result.Value!.GetType().GetProperties().Select(x=>x.Name));
    }

    [Fact]
    public async Task AdherenceWithoutActiveProgramReturnsNullRate()
    {
        await using var f=await Fixture.Create(UserRole.Owner);var c=f.AddClient(f.UserId);await f.Db.SaveChangesAsync();
        var result=Assert.IsType<OkObjectResult>(await f.Workouts.Adherence(c.Id,"week",null,default));
        Assert.Null(result.Value!.GetType().GetProperty("rate")!.GetValue(result.Value));
    }

    [Fact]
    public async Task MealPlanRowVersionConflictReturns409()
    {
        await using var f=await Fixture.Create(UserRole.Owner);var plan=new MealPlan{Id=Guid.NewGuid(),TenantId=f.TenantId,Title="Draft",Meals="[]",RowVersion=[1,2,3]};f.Db.MealPlans.Add(plan);await f.Db.SaveChangesAsync();
        var result=await f.Meals.Update(plan.Id,new MealPlanUpdate("Changed",[3,2,1]),default);
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task AssignedCoachCannotAccessAnotherClientsWorkoutLogs()
    {
        await using var f=await Fixture.Create(UserRole.Staff);var c=f.AddClient(Guid.NewGuid());await f.Db.SaveChangesAsync();
        Assert.IsType<NotFoundResult>(await f.Workouts.ClientLogs(c.Id,null,null,1,50,default));
    }

    [Fact]
    public async Task DashboardWithoutPlanOrSubscriptionReturnsNullSections()
    {
        await using var f=await Fixture.Create(UserRole.Client);f.AddClient(f.UserId);await f.Db.SaveChangesAsync();
        var result=Assert.IsType<OkObjectResult>(await f.Self.Dashboard(default));
        Assert.Null(result.Value!.GetType().GetProperty("program")!.GetValue(result.Value));
        Assert.Null(result.Value.GetType().GetProperty("subscription")!.GetValue(result.Value));
    }

    [Fact]
    public async Task MealPlanAssignArchivesPreviousActivePlan()
    {
        await using var f=await Fixture.Create(UserRole.Owner);var client=f.AddClient(f.UserId);
        var old=new MealPlan{Id=Guid.NewGuid(),TenantId=f.TenantId,ClientId=client.Id,Title="Old",Meals="[]",Status="Active"};
        var next=new MealPlan{Id=Guid.NewGuid(),TenantId=f.TenantId,Title="New",Meals="[]",Status="Draft"};f.Db.MealPlans.AddRange(old,next);await f.Db.SaveChangesAsync();
        var result=await f.Meals.Assign(next.Id,new MealPlanAssign(client.Id),default);
        Assert.IsType<NoContentResult>(result);Assert.Equal("Archived",old.Status);Assert.Equal("Active",next.Status);
    }

    [Fact]
    public async Task MealTemplateCannotBeAssigned()
    {
        await using var f=await Fixture.Create(UserRole.Owner);var client=f.AddClient(f.UserId);var template=new MealPlan{Id=Guid.NewGuid(),TenantId=f.TenantId,Title="Template",Meals="[]",IsTemplate=true,Status="Template"};f.Db.MealPlans.Add(template);await f.Db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await f.Meals.Assign(template.Id,new MealPlanAssign(client.Id),default));
    }

    [Fact]
    public async Task WorkoutLogOutside48HoursCannotBeEdited()
    {
        await using var f=await Fixture.Create(UserRole.Client);var c=f.AddClient(null);var exercise=Guid.NewGuid();
        var p=new CoachPulse.Domain.Entities.Program{Id=Guid.NewGuid(),TenantId=f.TenantId,ClientId=c.Id,CoachId=f.UserId,Title="Active",Status="Active",StartDate=DateTime.UtcNow.AddDays(-4),DurationDays=30};
        var e=new Exercise{Id=Guid.NewGuid(),Name="Press",MuscleGroup="Chest"};var pe=new ProgramExercise{Id=exercise,ProgramId=p.Id,ExerciseId=e.Id,Sets=3,Reps="8",Day=1};f.Db.Exercises.Add(e);f.Db.Programs.Add(p);f.Db.ProgramExercises.Add(pe);
        var log=new WorkoutLog{Id=Guid.NewGuid(),TenantId=f.TenantId,ClientId=c.Id,ProgramExerciseId=exercise,SetNumber=1,Reps=8,WeightKg=10,PerformedAt=DateTime.UtcNow.AddDays(-3),PerformedOn=DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),CreatedAt=DateTime.UtcNow.AddHours(-49)};f.Db.WorkoutLogs.Add(log);await f.Db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await f.Workouts.Update(log.Id,new WorkoutLogUpdate(10,12),default));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(CoachPulseDbContext db, State state, Guid tenant, Guid user, MyExperienceController self, WorkoutLogsController workouts, MealPlansController meals)
        { Db=db;State=state;TenantId=tenant;UserId=user;Self=self;Workouts=workouts;Meals=meals; }
        public CoachPulseDbContext Db {get;} public State State{get;} public Guid TenantId{get;} public Guid UserId{get;} public MyExperienceController Self{get;} public WorkoutLogsController Workouts{get;} public MealPlansController Meals{get;}
        public static async Task<Fixture> Create(UserRole role)
        {
            var state=new State();var tenant=Guid.NewGuid();var user=Guid.NewGuid();state.SetTenant(tenant);
            var db=new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,state);await db.Database.EnsureCreatedAsync();
            db.Tenants.Add(new Tenant{Id=tenant,Name="Test",Slug=tenant.ToString("N"),Status="Active"});db.Users.Add(new User{Id=user,TenantId=tenant,Email="user@example.test",Role=role});await db.SaveChangesAsync();
            var self=new MyExperienceController(db);var workouts=new WorkoutLogsController(db,new ClientAccessService(db));var meals=new MealPlansController(db,new ClientAccessService(db));
            var fx=new Fixture(db,state,tenant,user,self,workouts,meals);fx.SetIdentity(role);return fx;
        }
        public Client AddClient(Guid? coach){var client=new Client{Id=Guid.NewGuid(),TenantId=TenantId,UserId=UserId,CoachId=coach};Db.Clients.Add(client);return client;}
        private void SetIdentity(UserRole role){var ctx=new DefaultHttpContext();ctx.User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,UserId.ToString()),new Claim("tenantId",TenantId.ToString()),new Claim(ClaimTypes.Role,role.ToString())],"test"));var controllerContext=new ControllerContext{HttpContext=ctx};Self.ControllerContext=controllerContext;Workouts.ControllerContext=controllerContext;Meals.ControllerContext=controllerContext;}
        public ValueTask DisposeAsync()=>Db.DisposeAsync();
    }
    private sealed class State:ICurrentTenantService{public Guid? TenantId{get;private set;}public void SetTenant(Guid id)=>TenantId=id;public void Clear()=>TenantId=null;}
}
