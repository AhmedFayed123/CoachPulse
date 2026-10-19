using System.Security.Claims;
using CoachPulse.Application.DTOs.Coach;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Controllers;

[ApiController]
[Route("api/exercises")]
[Authorize]
public sealed class ExercisesController(CoachPulseDbContext db) : ControllerBase
{
    private Guid? TenantId => Guid.TryParse(User.FindFirstValue("tenantId"), out var id) ? id : null;
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private string? Role => User.FindFirstValue(ClaimTypes.Role);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? muscleGroup, [FromQuery] string? equipment,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (TenantId is null) return Unauthorized();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = db.Exercises.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(muscleGroup)) q = q.Where(x => x.MuscleGroup == muscleGroup);
        if (!string.IsNullOrWhiteSpace(equipment)) q = q.Where(x => x.Equipment == equipment);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Name.Contains(search));
        var total = await q.CountAsync();
        var rows = await q.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.Name, x.MuscleGroup, x.Equipment, x.VideoUrl, x.VideoFileId, x.TenantId }).ToListAsync();
        return Ok(new { items = rows, page, pageSize, total });
    }

    [HttpGet("meta")]
    public async Task<IActionResult> Meta()
    {
        var q = db.Exercises.AsNoTracking();
        return Ok(new { muscleGroups = await q.Select(x => x.MuscleGroup).Distinct().OrderBy(x => x).ToListAsync(),
            equipment = await q.Where(x => x.Equipment != null).Select(x => x.Equipment!).Distinct().OrderBy(x => x).ToListAsync() });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var ex = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return ex is null ? NotFound() : Ok(new { ex.Id, ex.Name, ex.MuscleGroup, ex.Equipment, ex.VideoUrl, ex.VideoFileId, ex.TenantId });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ExerciseRequest req)
    {
        if (!Valid(req)) return UnprocessableEntity(new ProblemDetails { Status = 422, Title = "Invalid exercise", Detail = "Name and muscleGroup are required; videoFileId or videoUrl must be valid." });
        var ex = await db.Exercises.FirstOrDefaultAsync(x => x.Id == id);
        if (ex is null) return NotFound();
        if (ex.TenantId is null && Role != "SuperAdmin") return StatusCode(403);
        if (ex.TenantId is not null && (TenantId != ex.TenantId || Role is not ("TenantOwner" or "CoachStaff"))) return NotFound();
        if (req.VideoFileId is not null && !await ValidExerciseVideo(req.VideoFileId.Value)) return UnprocessableEntity("videoFileId must identify an exercise-video file in this tenant.");
        ex.Name = req.Name.Trim(); ex.MuscleGroup = req.MuscleGroup.Trim(); ex.Equipment = req.Equipment?.Trim(); ex.VideoUrl = req.VideoUrl; ex.VideoFileId = req.VideoFileId;
        await db.SaveChangesAsync();
        return Ok(new { ex.Id, ex.Name, ex.MuscleGroup, ex.Equipment, ex.VideoUrl, ex.VideoFileId, ex.TenantId });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ex = await db.Exercises.FirstOrDefaultAsync(x => x.Id == id);
        if (ex is null) return NotFound();
        if (ex.TenantId is null && Role != "SuperAdmin") return StatusCode(403);
        if (ex.TenantId is not null && (TenantId != ex.TenantId || Role != "TenantOwner")) return NotFound();
        if (await db.ProgramExercises.AnyAsync(x => x.ExerciseId == id)) return Conflict(new ProblemDetails { Status = 409, Title = "Exercise is in use", Detail = "Remove it from every program before deletion." });
        ex.IsDeleted = true; await db.SaveChangesAsync(); return NoContent();
    }

    private bool Valid(ExerciseRequest r) => !string.IsNullOrWhiteSpace(r.Name) && !string.IsNullOrWhiteSpace(r.MuscleGroup) &&
        (r.VideoUrl is null || Uri.TryCreate(r.VideoUrl, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps);
    private async Task<bool> ValidExerciseVideo(Guid fileId) => TenantId is not null && await db.StoredFiles.IgnoreQueryFilters().AnyAsync(f => f.Id == fileId && f.TenantId == TenantId && f.Purpose == "exercise-video" && !f.IsDeleted);
}

public sealed class ExerciseRequest
{
    public string Name { get; set; } = "";
    public string MuscleGroup { get; set; } = "";
    public string? Equipment { get; set; }
    public string? VideoUrl { get; set; }
    public Guid? VideoFileId { get; set; }
}
