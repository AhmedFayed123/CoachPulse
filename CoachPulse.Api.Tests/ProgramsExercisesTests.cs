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

public sealed class ProgramsExercisesTests
{
    [Fact]
    public async Task ProgramUpdate_WithStaleRowVersion_Returns409() { await using var f = await Fixture.Create(); var p = f.AddProgram(); await f.Db.SaveChangesAsync(); Assert.IsType<ConflictObjectResult>(await f.Programs.Update(p.Id, new ProgramUpdateRequest { Title = "updated", DurationDays = 7, RowVersion = new byte[8] })); }
    [Fact]
    public async Task StaffCannotReadProgramOfUnassignedClient_Returns404() { await using var f = await Fixture.Create(UserRole.CoachStaff); var p = f.AddProgram(client: f.AddClient(Guid.NewGuid())); await f.Db.SaveChangesAsync(); Assert.IsType<NotFoundResult>(await f.Programs.Get(p.Id)); }
    [Fact]
    public async Task StaffCannotModifyGeneralExercise_Returns403() { await using var f = await Fixture.Create(UserRole.CoachStaff); var e = new Exercise { Id = Guid.NewGuid(), Name = "Squat", MuscleGroup = "Legs" }; f.Db.Exercises.Add(e); await f.Db.SaveChangesAsync(); Assert.IsType<StatusCodeResult>(await f.Exercises.Update(e.Id, new ExerciseRequest { Name = "Squat 2", MuscleGroup = "Legs" })); }
    [Fact]
    public async Task PrivateExerciseFromAnotherTenant_IsHidden() { await using var f = await Fixture.Create(); var e = new Exercise { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Name = "Other", MuscleGroup = "Back" }; f.Db.Exercises.Add(e); await f.Db.SaveChangesAsync(); Assert.IsType<NotFoundResult>(await f.Exercises.Get(e.Id)); }
    [Fact]
    public async Task ProgramFromAnotherTenant_IsHidden() { await using var f = await Fixture.Create(); var p = new CoachPulse.Domain.Entities.Program { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), CoachId = f.UserId, Title = "Other" }; f.Db.Programs.Add(p); await f.Db.SaveChangesAsync(); Assert.IsType<NotFoundResult>(await f.Programs.Get(p.Id)); }
    [Fact]
    public async Task UsedExerciseCannotBeDeleted_Returns409() { await using var f = await Fixture.Create(); var p = f.AddProgram(); var e = new Exercise { Id = Guid.NewGuid(), TenantId = f.TenantId, Name = "Row", MuscleGroup = "Back" }; f.Db.Exercises.Add(e); f.Db.ProgramExercises.Add(new ProgramExercise { Id = Guid.NewGuid(), ProgramId = p.Id, ExerciseId = e.Id, Sets = 3, Reps = "10", Day = 1 }); await f.Db.SaveChangesAsync(); Assert.IsType<ConflictObjectResult>(await f.Exercises.Delete(e.Id)); }
    [Fact]
    public async Task AssignArchivesPreviousActiveProgram() { await using var f = await Fixture.Create(); var client = f.AddClient(f.UserId); var previous = f.AddProgram(client, "Active"); var next = f.AddProgram(); await f.Db.SaveChangesAsync(); var result = await f.Programs.Assign(next.Id, new AssignProgramRequest { ClientId = client.Id, StartDate = DateTime.UtcNow.Date }); Assert.IsType<OkObjectResult>(result); Assert.Equal("Archived", previous.Status); Assert.Equal("Active", next.Status); }
    [Fact]
    public async Task AssigningTemplate_Returns409() { await using var f = await Fixture.Create(); var p = f.AddProgram(); p.IsTemplate = true; var client = f.AddClient(f.UserId); await f.Db.SaveChangesAsync(); Assert.IsType<ConflictObjectResult>(await f.Programs.Assign(p.Id, new AssignProgramRequest { ClientId = client.Id, StartDate = DateTime.UtcNow })); }
    [Fact]
    public async Task ReorderWithMissingIds_Returns422() { await using var f = await Fixture.Create(); var p = f.AddProgram(); var e = new Exercise { Id = Guid.NewGuid(), TenantId = f.TenantId, Name = "Row", MuscleGroup = "Back" }; f.Db.Exercises.Add(e); f.Db.ProgramExercises.Add(new ProgramExercise { Id = Guid.NewGuid(), ProgramId = p.Id, ExerciseId = e.Id, Sets = 3, Reps = "10", Day = 1 }); await f.Db.SaveChangesAsync(); var result = await f.Programs.Reorder(p.Id, new ReorderProgramExercisesRequest { DayNumber = 1, ProgramExerciseIds = [], RowVersion = p.RowVersion }); Assert.IsType<UnprocessableEntityObjectResult>(result); }
    [Fact]
    public async Task TodayWithNoExerciseForCurrentDay_ReturnsRestDay() { await using var f = await Fixture.Create(UserRole.Client); var client = new Client { Id = Guid.NewGuid(), TenantId = f.TenantId, UserId = f.UserId }; f.Db.Clients.Add(client); var p = f.AddProgram(client, "Active"); p.StartDate = DateTime.UtcNow.Date; p.DurationDays = 7; var e = new Exercise { Id = Guid.NewGuid(), Name = "Row", MuscleGroup = "Back" }; f.Db.Exercises.Add(e); f.Db.ProgramExercises.Add(new ProgramExercise { Id = Guid.NewGuid(), ProgramId = p.Id, ExerciseId = e.Id, Sets = 3, Reps = "10", Day = 2 }); await f.Db.SaveChangesAsync(); var result = Assert.IsType<OkObjectResult>(await f.My.GetToday()); Assert.True((bool)result.Value!.GetType().GetProperty("restDay")!.GetValue(result.Value)!); }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(CoachPulseDbContext db, TenantState tenant, Guid tenantId, Guid userId, ProgramsController programs, ExercisesController exercises, MyProgramController my) { Db = db; Tenant = tenant; TenantId = tenantId; UserId = userId; Programs = programs; Exercises = exercises; My = my; }
        public CoachPulseDbContext Db { get; } public TenantState Tenant { get; } public Guid TenantId { get; } public Guid UserId { get; } public ProgramsController Programs { get; } public ExercisesController Exercises { get; } public MyProgramController My { get; }
        public static async Task<Fixture> Create(UserRole role = UserRole.TenantOwner)
        {
            var ts = new TenantState(); var tid = Guid.NewGuid(); var uid = Guid.NewGuid(); ts.SetTenant(tid);
            var db = new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, ts); await db.Database.EnsureCreatedAsync();
            db.Tenants.Add(new Tenant { Id = tid, Name = "t", Slug = "t", Status = "Active", TimeZone = "UTC" });
            db.Users.Add(new User { Id = uid, TenantId = tid, Email = "coach@test", Role = role, PasswordHash = "x" }); await db.SaveChangesAsync();
            var claims = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, uid.ToString()), new Claim("tenantId", tid.ToString()), new Claim(ClaimTypes.Role, role.ToString())], "test"));
            ControllerContext Ctx() => new() { HttpContext = new DefaultHttpContext { User = claims } };
            var programs = new ProgramsController(db, new ClientAccessService(db)) { ControllerContext = Ctx() };
            var exercises = new ExercisesController(db) { ControllerContext = Ctx() }; var my = new MyProgramController(db) { ControllerContext = Ctx() };
            return new Fixture(db, ts, tid, uid, programs, exercises, my);
        }
        public Client AddClient(Guid coachId) { var u = new User { Id = Guid.NewGuid(), TenantId = TenantId, Email = $"{Guid.NewGuid():N}@test", Role = UserRole.Client }; Db.Users.Add(u); var c = new Client { Id = Guid.NewGuid(), TenantId = TenantId, UserId = u.Id, CoachId = coachId }; Db.Clients.Add(c); return c; }
        public CoachPulse.Domain.Entities.Program AddProgram(Client? client = null, string status = "Draft") { var p = new CoachPulse.Domain.Entities.Program { Id = Guid.NewGuid(), TenantId = TenantId, ClientId = client?.Id, CoachId = UserId, Title = "Program", Status = status, StartDate = DateTime.UtcNow.Date }; Db.Programs.Add(p); return p; }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
    private sealed class TenantState : ICurrentTenantService { public Guid? TenantId { get; private set; } public void SetTenant(Guid id) => TenantId = id; public void Clear() => TenantId = null; }
}
