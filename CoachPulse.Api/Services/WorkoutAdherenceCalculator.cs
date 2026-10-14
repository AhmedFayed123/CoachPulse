using CoachPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoachPulse.Api.Services;

public static class WorkoutAdherenceCalculator
{
    public sealed record Result(int Planned, int Completed, decimal? Rate);

    public static async Task<Result> CalculateAsync(CoachPulseDbContext db, Guid tenantId, Guid clientId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var program = await db.Programs.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && x.ClientId == clientId && x.Status == "Active" && !x.IsTemplate).OrderByDescending(x => x.StartDate).FirstOrDefaultAsync(ct);
        if (program?.StartDate is null) return new(0, 0, null);
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId, ct);
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(tenant?.TimeZone ?? "Africa/Cairo"); } catch { zone = TimeZoneInfo.Utc; }
        DateTime LocalDate(DateTime v) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(v, DateTimeKind.Utc), zone).Date;
        var assignment = LocalDate(program.StartDate.Value); var first = LocalDate(fromUtc); var last = LocalDate(toUtc);
        if (first < assignment) first = assignment;
        if (last < first) return new(0, 0, null);
        var perDay = await db.ProgramExercises.AsNoTracking().Where(x => x.ProgramId == program.Id).GroupBy(x => x.Day).Select(g => new { Day = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Day, x => x.Count, ct);
        var programExerciseIds = await db.ProgramExercises.AsNoTracking().Where(x => x.ProgramId == program.Id).Select(x => x.Id).ToListAsync(ct);
        var planned = 0;
        for (var d = first; d <= last && (d - assignment).Days < program.DurationDays; d = d.AddDays(1))
            if (perDay.TryGetValue((d - assignment).Days + 1, out var count)) planned += count;
        var logs = await db.WorkoutLogs.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && x.ClientId == clientId && programExerciseIds.Contains(x.ProgramExerciseId) && x.PerformedAt >= fromUtc && x.PerformedAt <= toUtc && !x.IsDeleted).Select(x => new { x.ProgramExerciseId, x.PerformedAt }).ToListAsync(ct);
        var completed = logs.GroupBy(x => LocalDate(x.PerformedAt)).Sum(g => g.Select(x => x.ProgramExerciseId).Distinct().Count());
        return new(planned, completed, planned == 0 ? null : Math.Min(1m, (decimal)completed / planned));
    }
}
