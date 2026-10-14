using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Data;
using CoachPulse.Application.DTOs;
using CoachPulse.Application.Interfaces;
using CoachPulse.Api.Services;
using CoachPulse.Domain.Entities;
using CoachPulse.Domain.Enums;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CoachPulse.Api.Hubs;

namespace CoachPulse.Api.Controllers;

[ApiController, Route("api/ai"), Authorize(Policy = "CoachOnly")]
public sealed class AiController(CoachPulseDbContext db, IAiClient ai, IClientAccessService access, IConfiguration config, ChurnScoringService churnScoring) : ControllerBase
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SemaphoreSlim> QuotaLocks = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, DateTime> LastRecompute = new();
    private Guid Tenant => Guid.TryParse(User.FindFirstValue("tenantId"), out var x) ? x : Guid.Empty;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var x) ? x : Guid.Empty;
    private UserRole Role => Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var r) ? r : UserRole.Client;
    private bool StaffOwns(Client c) => Role == UserRole.Owner || (Role == UserRole.Staff && c.CoachId == UserId);
    private IQueryable<Client> VisibleClients() => access.VisibleClients(Tenant, UserId, Role);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private IActionResult PythonUnavailable() => StatusCode(503, new ProblemDetails { Status = 503, Title = "AI service unavailable", Detail = "The internal AI service is temporarily unavailable. Please retry shortly." });
    private int DailyLimit => Math.Clamp(config.GetValue("Ai:DailyGenerationLimit", 20), 1, 10000);

    [HttpPost("generate-program")]
    public async Task<IActionResult> Generate(AiGenerateApiRequest request, CancellationToken ct)
    {
        if (request.DaysPerWeek is < 1 or > 7 || request.DurationWeeks is < 1 or > 24 || request.SessionMinutes is < 15 or > 240 || request.Language is not ("ar" or "en") ||
            request.Goal is not ("FatLoss" or "MuscleGain" or "Strength" or "GeneralFitness" or "Rehabilitation") || request.Level is not ("Beginner" or "Intermediate" or "Advanced")) return UnprocessableEntity("Invalid AI generation values.");
        var client = await VisibleClients().FirstOrDefaultAsync(x => x.Id == request.ClientId, ct);
        if (client is null) return NotFound();
        var exercises = await db.Exercises.AsNoTracking().Where(x => x.TenantId == null || x.TenantId == Tenant).Select(x => new AiExerciseOption(x.Id, x.Name, x.MuscleGroup, x.Equipment)).ToListAsync(ct);
        if (exercises.Count == 0) return UnprocessableEntity("No available exercises are configured for this tenant.");
        var input = new AiGenerateInput(client.Id, request.Goal, request.Level, request.DaysPerWeek, request.SessionMinutes, request.DurationWeeks, request.Equipment, request.Injuries, request.DietaryRestrictions, request.CaloriesTarget, request.Language, request.ExtraNotes, exercises);
        return await GenerateCore(input, client, null, ct);
    }

    private async Task<IActionResult> GenerateCore(AiGenerateInput input, Client client, Guid? parentId, CancellationToken ct)
    {
        var key = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var endpoint = parentId.HasValue ? "ai.regenerate" : "ai.generate";
        var bodyHash = Hash(JsonSerializer.Serialize(new { input, parentId }));
        if (!string.IsNullOrWhiteSpace(key))
        {
            var old = await db.IdempotencyKeys.FirstOrDefaultAsync(x => x.UserId == UserId && x.Endpoint == endpoint && x.Key == key && x.CreatedAt >= DateTime.UtcNow.AddHours(-24), ct);
            if (old is not null) return old.RequestHash == bodyHash ? Content(old.ResponseSnapshot, "application/json") : UnprocessableEntity("Idempotency-Key was already used with a different request.");
        }
        var gate = QuotaLocks.GetOrAdd(Tenant, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? quotaTransaction = null;
        try
        {
            if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            {
                quotaTransaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var bytes = SHA256.HashData(Tenant.ToByteArray());
                var lockId = BitConverter.ToInt64(bytes, 0);
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockId})", ct);
            }
            if (!string.IsNullOrWhiteSpace(key))
            {
                var replay = await db.IdempotencyKeys.FirstOrDefaultAsync(x => x.UserId == UserId && x.Endpoint == endpoint && x.Key == key && x.CreatedAt >= DateTime.UtcNow.AddHours(-24), ct);
                if (replay is not null) return replay.RequestHash == bodyHash ? Content(replay.ResponseSnapshot, "application/json") : UnprocessableEntity("Idempotency-Key was already used with a different request.");
            }
            var today = DateTime.UtcNow.Date;
            var used = await db.AiUsages.CountAsync(x => x.CreatedAt >= today && x.CreatedAt < today.AddDays(1), ct);
            if (used >= DailyLimit) { Response.Headers.RetryAfter = Math.Max(1, (int)(today.AddDays(1) - DateTime.UtcNow).TotalSeconds).ToString(); return StatusCode(429, new ProblemDetails { Status = 429, Title = "Daily AI generation quota exceeded" }); }
            AiWorkoutContent content;
            AiModelInfo modelInfo;
            try
            {
                modelInfo = await ai.GetModelInfoAsync(ct);
                content = parentId.HasValue ? await ai.RegenerateProgramAsync(new AiRegenerateInput(input, parentId.Value, input.ExtraNotes), ct) : await ai.GenerateProgramAsync(input, ct);
            }
            catch (HttpRequestException) { return PythonUnavailable(); }
            var serialized = Sanitize(content, input.DurationWeeks, client.Id);
            var promptVersion = config["Ai:PromptVersion"] ?? "program-v1";
            var entity = new AIGeneratedProgram { Id = Guid.NewGuid(), TenantId = Tenant, ClientId = client.Id, Prompt = JsonSerializer.Serialize(new { goal = input.Goal, level = input.Level, daysPerWeek = input.DaysPerWeek, sessionMinutes = input.SessionMinutes, durationWeeks = input.DurationWeeks, equipment = input.Equipment, injuries = input.Injuries, dietaryRestrictions = input.DietaryRestrictions, caloriesTarget = input.CaloriesTarget, language = input.Language }), GeneratedContent = serialized, RequestedByUserId = UserId, ParentGeneratedProgramId = parentId, Status = "Draft", PromptVersion = promptVersion, ModelVersion = modelInfo.ModelVersion };
            db.AIGeneratedPrograms.Add(entity);
            db.AiUsages.Add(new AiUsage { Id = Guid.NewGuid(), TenantId = Tenant, UserId = UserId, GeneratedProgramId = entity.Id });
            using var contentDocument = JsonDocument.Parse(serialized);
            var contentRoot = contentDocument.RootElement;
            var result = JsonSerializer.Serialize(new { generatedProgramId = entity.Id, status = entity.Status, workoutPlan = contentRoot.GetProperty("workoutPlan").Clone(), mealPlan = contentRoot.GetProperty("mealPlan").Clone(), unmatchedExercises = contentRoot.GetProperty("unmatchedExercises").Clone(), rowVersion = Convert.ToBase64String(entity.RowVersion), promptVersion = entity.PromptVersion, modelVersion = entity.ModelVersion, disclaimer = "AI-generated draft. A qualified coach must review before use." });
            if (!string.IsNullOrWhiteSpace(key)) db.IdempotencyKeys.Add(new IdempotencyKey { Id = Guid.NewGuid(), TenantId = Tenant, UserId = UserId, Endpoint = endpoint, Key = key, RequestHash = bodyHash, ResponseSnapshot = result });
            await db.SaveChangesAsync(ct);
            if (quotaTransaction is not null) await quotaTransaction.CommitAsync(ct);
            return StatusCode(StatusCodes.Status201Created, JsonDocument.Parse(result).RootElement);
        }
        finally { if (quotaTransaction is not null) await quotaTransaction.DisposeAsync(); gate.Release(); }
    }

    private string Sanitize(AiWorkoutContent content, int durationWeeks, Guid clientId)
    {
        var root = JsonNode.Parse(JsonSerializer.Serialize(content, new JsonSerializerOptions(JsonSerializerDefaults.Web)))!.AsObject();
        var allowed = db.Exercises.AsNoTracking().Where(x => x.TenantId == null || x.TenantId == Tenant).Select(x => x.Id).ToHashSet();
        var unmatched = root["unmatchedExercises"]?.AsArray() ?? new JsonArray();
        var weeks = root["workoutPlan"]?["weeks"]?.AsArray();
        if (weeks is not null)
            foreach (var week in weeks)
                if (week?["days"] is JsonArray days)
                    foreach (var day in days)
                        if (day?["exercises"] is JsonArray items)
                            for (var i = items.Count - 1; i >= 0; i--)
                            {
                                var item = items[i];
                                if (Guid.TryParse(item?["exerciseId"]?.ToString(), out var id) && allowed.Contains(id)) continue;
                                var name = item?["name"]?.ToString() ?? "unknown exercise";
                                unmatched.Add(name); items.RemoveAt(i);
                            }
        root["unmatchedExercises"] = unmatched;
        return root.ToJsonString();
    }

    [HttpGet("generated-programs")]
    public async Task<IActionResult> List([FromQuery] Guid? clientId, CancellationToken ct)
    {
        var q = db.AIGeneratedPrograms.AsNoTracking().Where(x => x.TenantId == Tenant);
        if (Role == UserRole.Staff) q = q.Where(x => VisibleClients().Any(c => c.Id == x.ClientId));
        if (clientId.HasValue) q = q.Where(x => x.ClientId == clientId);
        return Ok(await q.OrderByDescending(x => x.Id).Select(x => new { x.Id, x.ClientId, x.Status, x.PromptVersion, x.ModelVersion, x.RequestedByUserId, x.ParentGeneratedProgramId }).ToListAsync(ct));
    }
    [HttpGet("generated-programs/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => await Accessible(id, ct) is { } item ? Ok(item) : NotFound();
    private Task<AIGeneratedProgram?> Accessible(Guid id, CancellationToken ct) => db.AIGeneratedPrograms.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == Tenant && (Role != UserRole.Staff || VisibleClients().Any(c => c.Id == x.ClientId)), ct);

    [HttpPut("generated-programs/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, AiGeneratedProgramEdit edit, CancellationToken ct)
    {
        var p = await Accessible(id, ct); if (p is null) return NotFound(); if (p.Status != "Draft") return Conflict("Only drafts can be edited.");
        byte[] version; try { version = Convert.FromBase64String(edit.RowVersion); } catch { return UnprocessableEntity("rowVersion is required."); }
        if (!CryptographicOperations.FixedTimeEquals(version, p.RowVersion)) return Conflict(new ProblemDetails { Status = 409, Title = "Draft was modified by another request." });
        p.GeneratedContent = JsonSerializer.Serialize(new { workoutPlan = edit.WorkoutPlan, mealPlan = edit.MealPlan, unmatchedExercises = edit.UnmatchedExercises }); p.RowVersion = RandomNumberGenerator.GetBytes(8);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Conflict(); }
        return Ok(new { p.Id, p.Status, rowVersion = Convert.ToBase64String(p.RowVersion) });
    }

    [HttpPost("generated-programs/{id:guid}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid id, AiRegenerateApiRequest req, CancellationToken ct)
    {
        if (req.CoachNotes.Length is < 1 or > 2000) return UnprocessableEntity();
        var p = await Accessible(id, ct); if (p is null) return NotFound();
        using var prompt = JsonDocument.Parse(p.Prompt);
        var r = prompt.RootElement;
        var exercises = await db.Exercises.AsNoTracking().Where(x => x.TenantId == null || x.TenantId == Tenant).Select(x => new AiExerciseOption(x.Id, x.Name, x.MuscleGroup, x.Equipment)).ToListAsync(ct);
        var input = new AiGenerateInput(p.ClientId, r.GetProperty("goal").GetString()!, r.GetProperty("level").GetString()!, r.GetProperty("daysPerWeek").GetInt32(), r.GetProperty("sessionMinutes").GetInt32(), r.GetProperty("durationWeeks").GetInt32(), r.GetProperty("equipment").Deserialize<List<string>>() ?? [], r.GetProperty("injuries").Deserialize<List<string>>() ?? [], r.GetProperty("dietaryRestrictions").Deserialize<List<string>>() ?? [], r.TryGetProperty("caloriesTarget", out var calories) && calories.ValueKind == JsonValueKind.Number ? calories.GetInt32() : null, r.GetProperty("language").GetString()!, req.CoachNotes, exercises);
        var client = await VisibleClients().FirstAsync(x => x.Id == p.ClientId, ct);
        return await GenerateCore(input, client, p.Id, ct);
    }
    [HttpPut("generated-programs/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var p = await Accessible(id, ct); if (p is null) return NotFound();
        if (p.Status == "Approved" && p.ApprovedProgramId is { } existing) return Ok(new { programId = existing, p.MealPlanId });
        if (p.Status != "Draft") return Conflict("Only a draft can be approved.");
        using var doc = JsonDocument.Parse(p.GeneratedContent);
        if (doc.RootElement.GetProperty("unmatchedExercises").GetArrayLength() != 0) return Conflict("Resolve unmatchedExercises before approval.");
        var duration = JsonDocument.Parse(p.Prompt).RootElement.GetProperty("durationWeeks").GetInt32();
        var program = new CoachPulse.Domain.Entities.Program { Id = Guid.NewGuid(), TenantId = Tenant, ClientId = p.ClientId, CoachId = UserId, Title = "AI generated program", DurationDays = duration * 7, Status = "Draft" };
        db.Programs.Add(program);
        var weeks = doc.RootElement.GetProperty("workoutPlan").GetProperty("weeks");
        foreach (var week in weeks.EnumerateArray())
        foreach (var (day, index) in week.GetProperty("days").EnumerateArray().Select((x, i) => (x, i)))
        foreach (var (exercise, order) in day.GetProperty("exercises").EnumerateArray().Select((x, i) => (x, i)))
        {
            if (!Guid.TryParse(exercise.GetProperty("exerciseId").GetString(), out var exerciseId) || !await db.Exercises.AnyAsync(x => x.Id == exerciseId && (x.TenantId == null || x.TenantId == Tenant), ct)) return Conflict("Exercise is no longer available in this tenant.");
            db.ProgramExercises.Add(new ProgramExercise { Id = Guid.NewGuid(), ProgramId = program.Id, ExerciseId = exerciseId, Day = Math.Min(duration * 7, (week.GetProperty("week").GetInt32() - 1) * 7 + index + 1), SortOrder = order, Sets = exercise.GetProperty("sets").GetInt32(), Reps = exercise.GetProperty("reps").GetString()!, RestSeconds = exercise.GetProperty("restSeconds").GetInt32() });
        }
        var mealJson = doc.RootElement.GetProperty("mealPlan");
        var meal = new MealPlan { Id = Guid.NewGuid(), TenantId = Tenant, ClientId = p.ClientId, Meals = mealJson.GetRawText(), Title = "AI generated meal plan", Status = "Draft" };
        if (mealJson.TryGetProperty("days", out var mealDays) && mealDays.ValueKind == JsonValueKind.Array)
        foreach (var day in mealDays.EnumerateArray())
        if (day.TryGetProperty("meals", out var dayMeals) && dayMeals.ValueKind == JsonValueKind.Array)
        foreach (var item in dayMeals.EnumerateArray())
        {
            decimal Number(JsonElement source, string primary, string alternate = "") => source.TryGetProperty(primary, out var value) && value.TryGetDecimal(out var number) ? number : alternate.Length > 0 && source.TryGetProperty(alternate, out value) && value.TryGetDecimal(out number) ? number : 0m;
            var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
            meal.Items.Add(new Meal { Id = Guid.NewGuid(), Name = string.IsNullOrWhiteSpace(name) ? "Meal" : name, Calories = Number(item, "calories"), Protein = Number(item, "protein", "proteinG"), Carbs = Number(item, "carbs", "carbsG"), Fat = Number(item, "fat", "fatG") });
        }
        db.MealPlans.Add(meal); p.Status = "Approved"; p.ApprovedByCoach = true; p.ApprovedProgramId = program.Id; p.MealPlanId = meal.Id;
        p.RowVersion = RandomNumberGenerator.GetBytes(8);
        db.SecurityAuditLogs.Add(new SecurityAuditLog { Id = Guid.NewGuid(), TenantId = Tenant, UserId = UserId, EventType = "ai.program.approved", Identifier = id.ToString() });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { var current = await db.AIGeneratedPrograms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Status == "Approved", ct); return current is not null && current.ApprovedProgramId is { } programId ? Ok(new { programId, current.MealPlanId }) : Conflict(); }
        return Ok(new { programId = program.Id, mealPlanId = meal.Id });
    }
    [HttpPut("generated-programs/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct) { var p = await Accessible(id, ct); if (p is null) return NotFound(); if (p.Status != "Draft") return Conflict(); p.Status = "Rejected"; await db.SaveChangesAsync(ct); return NoContent(); }
    [HttpGet("usage"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Usage(CancellationToken ct) { var today = DateTime.UtcNow.Date; var used = await db.AiUsages.CountAsync(x => x.CreatedAt >= today && x.CreatedAt < today.AddDays(1), ct); return Ok(new { dailyLimit = DailyLimit, used, remaining = Math.Max(0, DailyLimit - used), resetsAt = today.AddDays(1) }); }

    [HttpGet("churn/clients/{id:guid}")]
    public async Task<IActionResult> Churn(Guid id, CancellationToken ct) { if (!await VisibleClients().AnyAsync(x => x.Id == id, ct)) return NotFound(); var score = await db.ChurnRiskLogs.Where(x => x.TenantId == Tenant && x.ClientId == id).OrderByDescending(x => x.ComputedAt).FirstOrDefaultAsync(ct); return score is null ? Ok(null) : Ok(new { score.ClientId, score.RiskScore, score.RiskLevel, TopFactors = JsonDocument.Parse(score.TopFactorsJson).RootElement, score.ModelVersion, score.ComputedAt }); }
    [HttpGet("churn/clients/{id:guid}/history")]
    public async Task<IActionResult> ChurnHistory(Guid id, CancellationToken ct) { if (!await VisibleClients().AnyAsync(x => x.Id == id, ct)) return NotFound(); return Ok(await db.ChurnRiskLogs.Where(x => x.TenantId == Tenant && x.ClientId == id).OrderByDescending(x => x.ComputedAt).Select(x => new { x.RiskScore, x.RiskLevel, x.TopFactorsJson, x.ModelVersion, x.ComputedAt }).ToListAsync(ct)); }
    [HttpPost("churn/recompute"), Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Recompute(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (LastRecompute.TryGetValue(Tenant, out var last) && now - last < TimeSpan.FromMinutes(10)) { Response.Headers.RetryAfter = Math.Ceiling((TimeSpan.FromMinutes(10) - (now - last)).TotalSeconds).ToString(); return StatusCode(429); }
        LastRecompute[Tenant] = now;
        try { if (!await ai.IsReadyAsync(ct)) { LastRecompute.TryRemove(Tenant, out _); return PythonUnavailable(); } return Ok(new { scored = await churnScoring.ScoreTenantAsync(Tenant, ct) }); }
        catch (HttpRequestException) { LastRecompute.TryRemove(Tenant, out _); return PythonUnavailable(); }
    }
}
