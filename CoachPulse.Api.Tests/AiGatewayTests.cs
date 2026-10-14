using System.Security.Claims;
using System.Text.Json;
using CoachPulse.Api.Controllers;
using CoachPulse.Application.DTOs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using CoachPulse.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.SignalR;
using CoachPulse.Api.Hubs;
using CoachPulse.Api.Services;
using Xunit;

namespace CoachPulse.Api.Tests;

public sealed class AiGatewayTests
{
    [Fact]
    public async Task GenerateStoresUsageOnlyAfterPythonSuccess_AndReplaysIdempotency()
    {
        await using var f = await Fixture.Create();
        f.Context.Request.Headers["Idempotency-Key"] = "same-generation";
        var request = f.Request();
        Assert.IsType<ObjectResult>(await f.Controller.Generate(request, CancellationToken.None));
        Assert.IsType<ContentResult>(await f.Controller.Generate(request, CancellationToken.None));
        Assert.Equal(1, await f.Db.AiUsages.CountAsync());
        Assert.Equal(1, await f.Db.AIGeneratedPrograms.CountAsync());
    }

    [Fact]
    public async Task GenerationQuotaRejectsAfterSuccessfulAllowance()
    {
        await using var f = await Fixture.Create(limit: 1);
        Assert.IsType<ObjectResult>(await f.Controller.Generate(f.Request(), CancellationToken.None));
        var rejected = Assert.IsType<ObjectResult>(await f.Controller.Generate(f.Request(), CancellationToken.None));
        Assert.Equal(StatusCodes.Status429TooManyRequests, rejected.StatusCode);
        Assert.Equal(1, await f.Db.AiUsages.CountAsync());
    }

    [Fact]
    public async Task PythonFailureDoesNotConsumeQuota()
    {
        await using var f = await Fixture.Create(failing: true);
        var response = Assert.IsType<ObjectResult>(await f.Controller.Generate(f.Request(), CancellationToken.None));
        Assert.Equal(503, response.StatusCode);
        Assert.Empty(await f.Db.AiUsages.ToListAsync());
    }

    [Fact]
    public async Task ProgressSummaryIsTenantScopedAndUsesGenerationQuota()
    {
        await using var f=await Fixture.Create(limit:1);
        var first=Assert.IsType<OkObjectResult>(await f.Controller.ProgressSummary(f.ClientId,new AiProgressSummaryRequest("en"),CancellationToken.None));
        Assert.NotNull(first.Value);Assert.Single(await f.Db.AiUsages.ToListAsync());
        var rejected=Assert.IsType<ObjectResult>(await f.Controller.ProgressSummary(f.ClientId,new AiProgressSummaryRequest("en"),CancellationToken.None));
        Assert.Equal(429,rejected.StatusCode);Assert.Single(await f.Db.AiUsages.ToListAsync());
        Assert.IsType<NotFoundResult>(await f.Controller.ProgressSummary(Guid.NewGuid(),new AiProgressSummaryRequest("en"),CancellationToken.None));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(CoachPulseDbContext db, TenantState state, Guid tenant, Guid user, Guid client, Guid exercise, AiController controller, DefaultHttpContext context)
        { Db = db; State = state; TenantId = tenant; UserId = user; ClientId = client; ExerciseId = exercise; Controller = controller; Context = context; }
        public CoachPulseDbContext Db { get; } public TenantState State { get; } public Guid TenantId { get; } public Guid UserId { get; } public Guid ClientId { get; } public Guid ExerciseId { get; } public AiController Controller { get; } public DefaultHttpContext Context { get; }
        public static async Task<Fixture> Create(int limit = 20, bool failing = false)
        {
            var state = new TenantState(); var tenant = Guid.NewGuid(); var user = Guid.NewGuid(); var clientId = Guid.NewGuid(); var exerciseId = Guid.NewGuid(); state.SetTenant(tenant);
            var db = new CoachPulseDbContext(new DbContextOptionsBuilder<CoachPulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, state); await db.Database.EnsureCreatedAsync();
            db.Tenants.Add(new Tenant { Id = tenant, Name = "T", Slug = "t", Status = "Active" });
            var clientUser = Guid.NewGuid();
            db.Users.AddRange(new User { Id = user, TenantId = tenant, Email = "coach@test.invalid", Role = UserRole.Owner }, new User { Id = clientUser, TenantId = tenant, Email = "client@test.invalid", Role = UserRole.Client });
            db.Clients.Add(new Client { Id = clientId, TenantId = tenant, UserId = clientUser, CoachId = user });
            db.Exercises.Add(new Exercise { Id = exerciseId, TenantId = tenant, Name = "Squat", MuscleGroup = "Legs", Equipment = "None" }); await db.SaveChangesAsync();
            var http = new DefaultHttpContext(); http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString()), new Claim("tenantId", tenant.ToString()), new Claim(ClaimTypes.Role, "Owner")], "test"));
            var service = new FakeAiClient(failing, exerciseId);
            var churn = new ChurnScoringService(db, service, new ChurnFeatureBuilder(db), new NullHub(), NullLogger<ChurnScoringService>.Instance);
            var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:DailyGenerationLimit"] = limit.ToString(), ["Ai:InternalApiKey"] = "test" }).Build();
            var controller = new AiController(db, service, new ClientAccessService(db), cfg, churn) { ControllerContext = new ControllerContext { HttpContext = http } };
            return new Fixture(db, state, tenant, user, clientId, exerciseId, controller, http);
        }
        public AiGenerateApiRequest Request() => new(ClientId, "FatLoss", "Beginner", 1, 45, 1, [], [], [], null, "en", "");
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class TenantState : ICurrentTenantService { public Guid? TenantId { get; private set; } public void SetTenant(Guid id) => TenantId = id; public void Clear() => TenantId = null; }
    private sealed class FakeAiClient(bool failing, Guid exerciseId) : IAiClient
    {
        public Task<AiModelInfo> GetModelInfoAsync(CancellationToken ct) => Task.FromResult(new AiModelInfo("fake-v1", 1, 1, 1, "synthetic"));
        public Task<bool> IsReadyAsync(CancellationToken ct) => Task.FromResult(!failing);
        public Task<AiWorkoutContent> GenerateProgramAsync(AiGenerateInput request, CancellationToken ct) => Task.FromResult(Content());
        public Task<AiWorkoutContent> RegenerateProgramAsync(AiRegenerateInput request, CancellationToken ct) => Task.FromResult(Content());
        private AiWorkoutContent Content() { if (failing) throw new HttpRequestException("down"); using var d = JsonDocument.Parse($"{{\"weeks\":[{{\"week\":1,\"days\":[{{\"day\":\"Sunday\",\"focus\":\"strength\",\"exercises\":[{{\"exerciseId\":\"{exerciseId}\",\"name\":\"Squat\",\"sets\":3,\"reps\":\"10-12\",\"restSeconds\":60,\"notes\":\"\"}}]}}]}}]}}"); using var m = JsonDocument.Parse("{\"dailyCalories\":2000,\"macros\":{\"proteinG\":100,\"carbsG\":200,\"fatG\":60},\"days\":[{\"day\":\"Sunday\",\"meals\":[]}]}"); return new AiWorkoutContent(d.RootElement.Clone(), m.RootElement.Clone(), []); }
        public Task<AiChurnResponse> PredictChurnAsync(AiChurnRequest request, CancellationToken ct) => throw new NotImplementedException();
        public Task<AiChurnBatchResponse> PredictChurnBatchAsync(IReadOnlyList<AiChurnRequest> requests, CancellationToken ct) => throw new NotImplementedException();
        public Task<AiProgressSummary> CreateProgressSummaryAsync(AiProgressSummaryInput request, CancellationToken ct) => Task.FromResult(new AiProgressSummary("summary", [], []));
    }
    private sealed class NullHub : IHubContext<NotificationsHub> { public IHubClients Clients => throw new NotImplementedException(); public IGroupManager Groups => throw new NotImplementedException(); }
}
