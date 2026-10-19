using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CoachPulse.Application.Interfaces;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoachProgram = CoachPulse.Domain.Entities.Program;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Authorize(Policy = "CoachOnly")]
public sealed class ProgramsController(CoachPulseDbContext db, IClientAccessService clientAccess) : ControllerBase
{
    private Guid? TenantId => Guid.TryParse(User.FindFirstValue("tenantId"), out var id) ? id : null;
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private string? Role => User.FindFirstValue(ClaimTypes.Role);
    private bool IsOwner => Role == "TenantOwner";

    [HttpGet("/api/programs/{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var p = await AccessiblePrograms().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? NotFound() : Ok(await ProgramView(p));
    }

    [HttpPut("/api/programs/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProgramUpdateRequest req)
    {
        if (req.RowVersion is not { Length: > 0 }) return UnprocessableEntity(new ProblemDetails { Status = 422, Title = "rowVersion is required" });
        var p = await AccessiblePrograms().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (p.Status == "Archived") return Conflict("Archived programs are read-only.");
        if (!Equal(p.RowVersion, req.RowVersion)) return VersionConflict();
        if (string.IsNullOrWhiteSpace(req.Title) || req.DurationDays is < 1 or > 365) return UnprocessableEntity("Title and valid durationDays are required.");
        p.Title = req.Title.Trim(); p.DurationDays = req.DurationDays; Bump(p);
        return await SaveProgram(p);
    }

    [HttpDelete("/api/programs/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, ProgramVersionRequest req)
    {
        if (req.RowVersion is not { Length: > 0 }) return UnprocessableEntity(new ProblemDetails { Status = 422, Title = "rowVersion is required" });
        var p = await AccessiblePrograms().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (p.Status == "Active" && p.ClientId != null) return Conflict("Archive the assigned program before deleting it.");
        if (!Equal(p.RowVersion, req.RowVersion)) return VersionConflict();
        p.IsDeleted = true; Bump(p); Audit("program.delete", p.Id); try { await db.SaveChangesAsync(); return NoContent(); } catch (DbUpdateConcurrencyException) { return VersionConflict(); }
    }

    [HttpPut("/api/programs/{id:guid}/exercises/{programExerciseId:guid}")]
    public async Task<IActionResult> UpdateExercise(Guid id, Guid programExerciseId, ProgramExerciseUpdateRequest req)
    {
        var p = await MutableProgram(id, req.RowVersion); if (p.Error is not null) return p.Error;
        var item = await db.ProgramExercises.FirstOrDefaultAsync(x => x.Id == programExerciseId && x.ProgramId == id);
        if (item is null) return NotFound();
        if (!ValidItem(req.Sets, req.Reps, req.RestSeconds, req.DayNumber, p.Program!.DurationDays)) return UnprocessableEntity("Sets, reps, restSeconds, or dayNumber is invalid.");
        item.Sets = req.Sets; item.Reps = req.Reps; item.RestSeconds = req.RestSeconds; item.Day = req.DayNumber; Bump(p.Program!);
        try { await db.SaveChangesAsync(); return Ok(new { item.Id, item.ProgramId, item.ExerciseId, item.Sets, item.Reps, item.RestSeconds, dayNumber = item.Day, p.Program!.RowVersion }); } catch (DbUpdateConcurrencyException) { return VersionConflict(); }
    }

    [HttpDelete("/api/programs/{id:guid}/exercises/{programExerciseId:guid}")]
    public async Task<IActionResult> DeleteExercise(Guid id, Guid programExerciseId, ProgramVersionRequest req)
    {
        var p = await MutableProgram(id, req.RowVersion); if (p.Error is not null) return p.Error;
        var item = await db.ProgramExercises.FirstOrDefaultAsync(x => x.Id == programExerciseId && x.ProgramId == id);
        if (item is null) return NotFound(); db.ProgramExercises.Remove(item); Bump(p.Program!);
        try { await db.SaveChangesAsync(); return NoContent(); } catch (DbUpdateConcurrencyException) { return VersionConflict(); }
    }

    [HttpPut("/api/programs/{id:guid}/exercises/reorder")]
    public async Task<IActionResult> Reorder(Guid id, ReorderProgramExercisesRequest req)
    {
        var p = await MutableProgram(id, req.RowVersion); if (p.Error is not null) return p.Error;
        if (req.DayNumber < 1 || req.DayNumber > p.Program!.DurationDays || req.ProgramExerciseIds is null || req.ProgramExerciseIds.Distinct().Count() != req.ProgramExerciseIds.Count)
            return UnprocessableEntity("The day and exercise IDs are invalid.");
        var items = await db.ProgramExercises.Where(x => x.ProgramId == id && x.Day == req.DayNumber).ToListAsync();
        if (items.Count != req.ProgramExerciseIds.Count || items.Any(x => !req.ProgramExerciseIds.Contains(x.Id))) return UnprocessableEntity("The list must contain every exercise for this program day exactly once.");
        for (var i = 0; i < req.ProgramExerciseIds.Count; i++) items.Single(x => x.Id == req.ProgramExerciseIds[i]).SortOrder = i;
        Bump(p.Program!); try { await db.SaveChangesAsync(); return Ok(new { dayNumber = req.DayNumber, programExerciseIds = req.ProgramExerciseIds, rowVersion = p.Program.RowVersion }); } catch (DbUpdateConcurrencyException) { return VersionConflict(); }
    }

    [HttpPost("/api/programs/{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id)
    {
        var p = await AccessiblePrograms().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        var items = await db.ProgramExercises.AsNoTracking().Where(x => x.ProgramId == id).ToListAsync();
        var copy = new CoachProgram { Id = Guid.NewGuid(), TenantId = p.TenantId, CoachId = UserId!.Value, Title = "نسخة من " + p.Title, Status = "Draft", DurationDays = p.DurationDays, RowVersion = RandomNumberGenerator.GetBytes(8) };
        db.Programs.Add(copy);
        foreach (var e in items) db.ProgramExercises.Add(new ProgramExercise { Id = Guid.NewGuid(), ProgramId = copy.Id, ExerciseId = e.ExerciseId, Sets = e.Sets, Reps = e.Reps, RestSeconds = e.RestSeconds, Day = e.Day, SortOrder = e.SortOrder });
        await db.SaveChangesAsync(); return Created($"/api/programs/{copy.Id}", await ProgramView(copy));
    }

    [HttpPut("/api/programs/{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, AssignProgramRequest req)
    {
        var p = await AccessiblePrograms().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (p.IsTemplate) return Conflict("Create a program from the template before assignment.");
        if (p.Status == "Archived") return Conflict("Archived programs cannot be assigned.");
        if (req.ClientId == Guid.Empty || req.StartDate == default) return UnprocessableEntity("clientId and startDate are required.");
        IQueryable<Client> clientQ = db.Clients.Where(c => c.Id == req.ClientId && c.TenantId == TenantId);
        if (Role == "CoachStaff") clientQ = clientQ.Where(c => c.CoachId == UserId);
        var client = await clientQ.FirstOrDefaultAsync(); if (client is null) return NotFound();
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == TenantId);
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(tenant?.TimeZone ?? "Africa/Cairo"); }
        catch (TimeZoneNotFoundException) { return UnprocessableEntity("Tenant timezone is invalid."); }
        var startDateUtc = req.StartDate.Kind switch
        {
            DateTimeKind.Utc => req.StartDate,
            DateTimeKind.Local => req.StartDate.ToUniversalTime(),
            _ => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(req.StartDate.Date, DateTimeKind.Unspecified), zone)
        };
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        try
        {
            var other = await db.Programs.Where(x => x.ClientId == client.Id && x.Status == "Active" && x.Id != id && !x.IsTemplate).ToListAsync();
            foreach (var old in other) { old.Status = "Archived"; Bump(old); Audit("program.archive", old.Id); }
            if (other.Count > 0) await db.SaveChangesAsync();
            p.ClientId = client.Id; p.StartDate = startDateUtc; p.Status = "Active"; p.CoachId = UserId!.Value; Bump(p); Audit("program.assign", p.Id);
            await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException) { return VersionConflict(); }
        catch (DbUpdateException) { return Conflict(new ProblemDetails { Status = 409, Title = "Program assignment conflict", Detail = "Another active program was assigned concurrently." }); }
        return Ok(await ProgramView(p));
    }

    [HttpPut("/api/programs/{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, ProgramVersionRequest req)
    {
        var p = await AccessiblePrograms().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound(); if (p.IsTemplate) return Conflict("Template cannot be archived.");
        if (req.RowVersion is not { Length: > 0 }) return UnprocessableEntity(new ProblemDetails { Status = 422, Title = "rowVersion is required" });
        if (!Equal(p.RowVersion, req.RowVersion)) return VersionConflict();
        p.Status = "Archived"; Bump(p); Audit("program.archive", p.Id); try { await db.SaveChangesAsync(); return Ok(await ProgramView(p)); } catch (DbUpdateConcurrencyException) { return VersionConflict(); }
    }

    [HttpGet("/api/clients/{clientId:guid}/programs")]
    public async Task<IActionResult> ClientPrograms(Guid clientId)
    {
        if (!Enum.TryParse<CoachPulse.Domain.Enums.UserRole>(Role, out var parsedRole)) return Unauthorized();
        var clients = clientAccess.VisibleClients(TenantId!.Value, UserId!.Value, parsedRole);
        if (!await clients.AnyAsync(c => c.Id == clientId)) return NotFound();
        var ps = await db.Programs.AsNoTracking().Where(p => p.ClientId == clientId && !p.IsTemplate && p.Status != "Draft").OrderByDescending(p => p.StartDate)
            .Select(p => new { p.Id, p.Title, p.Status, p.StartDate, p.DurationDays, p.RowVersion }).ToListAsync();
        return Ok(ps);
    }

    private IQueryable<CoachProgram> AccessiblePrograms()
    {
        var q = db.Programs.Where(x => x.TenantId == TenantId && !x.IsDeleted);
        if (Role == "CoachStaff") q = q.Where(p => p.IsTemplate || (p.ClientId == null && p.CoachId == UserId) || (p.ClientId != null && db.Clients.Any(c => c.Id == p.ClientId && c.CoachId == UserId)));
        return q;
    }
    private async Task<object> ProgramView(CoachProgram p) => new { p.Id, p.Title, p.ClientId, p.CoachId, p.IsTemplate, p.Status, p.StartDate, p.DurationDays, p.RowVersion,
        exercises = await db.ProgramExercises.AsNoTracking().Where(x => x.ProgramId == p.Id).OrderBy(x => x.Day).ThenBy(x => x.SortOrder)
            .Select(x => new { x.Id, x.ExerciseId, ExerciseName = x.Exercise.Name, x.Exercise.MuscleGroup, x.Sets, x.Reps, x.RestSeconds, dayNumber = x.Day, x.SortOrder }).ToListAsync() };
    private async Task<(CoachProgram? Program, IActionResult? Error)> MutableProgram(Guid id, byte[]? rowVersion)
    {
        if (rowVersion is not { Length: > 0 }) return (null, UnprocessableEntity(new ProblemDetails { Status = 422, Title = "rowVersion is required" }));
        var p = await AccessiblePrograms().FirstOrDefaultAsync(x => x.Id == id); if (p is null) return (null, NotFound());
        if (p.Status == "Archived") return (null, Conflict("Archived programs are read-only."));
        if (!Equal(p.RowVersion, rowVersion)) return (null, VersionConflict()); return (p, null);
    }
    private IActionResult VersionConflict() => Conflict(new ProblemDetails { Status = 409, Title = "Program version conflict", Detail = "Reload the program and retry with the latest rowVersion." });
    private async Task<IActionResult> SaveProgram(CoachProgram p)
    { try { await db.SaveChangesAsync(); return Ok(await ProgramView(p)); } catch (DbUpdateConcurrencyException) { return VersionConflict(); } }
    private static bool Equal(byte[] a, byte[] b) => a.AsSpan().SequenceEqual(b);
    private static void Bump(CoachProgram p) { p.RowVersion = BitConverter.GetBytes(BitConverter.ToInt64(p.RowVersion.Length == 8 ? p.RowVersion : new byte[8]) + 1); }
    private static bool ValidItem(int sets, string reps, int rest, int day, int duration) => sets is >= 1 and <= 20 && Regex.IsMatch(reps, @"^\d{1,3}(-\d{1,3})?$") && rest is >= 0 and <= 600 && day >= 1 && day <= duration;
    private void Audit(string eventType, Guid programId) => db.SecurityAuditLogs.Add(new SecurityAuditLog { Id = Guid.NewGuid(), UserId = UserId, TenantId = TenantId, EventType = eventType, Identifier = programId.ToString(), CreatedAt = DateTime.UtcNow });
}

public sealed class ProgramUpdateRequest { public string Title { get; set; } = ""; public int DurationDays { get; set; } public byte[]? RowVersion { get; set; } }
public sealed class ProgramVersionRequest { public byte[]? RowVersion { get; set; } }
public sealed class ProgramExerciseUpdateRequest { public int Sets { get; set; } public string Reps { get; set; } = ""; public int RestSeconds { get; set; } public int DayNumber { get; set; } public byte[]? RowVersion { get; set; } }
public sealed class ReorderProgramExercisesRequest { public int DayNumber { get; set; } public List<Guid> ProgramExerciseIds { get; set; } = []; public byte[]? RowVersion { get; set; } }
public sealed class AssignProgramRequest { public Guid ClientId { get; set; } public DateTime StartDate { get; set; } }

