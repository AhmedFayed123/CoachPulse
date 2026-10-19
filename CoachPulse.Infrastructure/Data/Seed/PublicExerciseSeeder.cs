using System.Globalization;
using Microsoft.EntityFrameworkCore;
using CoachPulse.Domain.Entities;
using CoachPulse.Infrastructure.Persistence;

namespace CoachPulse.Infrastructure.Data.Seed;

public static class PublicExerciseSeeder
{
    public static async Task<int> SeedCsvAsync(CoachPulseDbContext db, string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Exercise seed CSV was not found.", path);
        var lines = await File.ReadAllLinesAsync(path, ct);
        if (lines.Length < 2 || !Parse(lines[0]).SequenceEqual(new[] { "name", "muscleGroup", "equipment", "videoUrl" }, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Exercise seed CSV header must be name,muscleGroup,equipment,videoUrl.");

        var known = await db.Exercises.IgnoreQueryFilters().Where(x => x.TenantId == null)
            .Select(x => x.Name.ToLower()).ToListAsync(ct);
        var names = known.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pending = new List<Exercise>();
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var row = Parse(lines[i]);
            if (row.Count != 4) throw new InvalidDataException($"Invalid exercise seed row {i + 1}: expected four columns.");
            var name = row[0].Trim(); var muscle = row[1].Trim(); var equipment = row[2].Trim(); var video = row[3].Trim();
            if (name.Length is 0 or > 200 || muscle.Length is 0 or > 100 || equipment.Length > 100 ||
                (!string.IsNullOrEmpty(video) && (!Uri.TryCreate(video, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))))
                throw new InvalidDataException($"Invalid exercise seed row {i + 1}: field validation failed.");
            if (!names.Add(name)) continue;
            pending.Add(new Exercise { Id = Guid.NewGuid(), TenantId = null, Name = name, MuscleGroup = muscle,
                Equipment = string.IsNullOrWhiteSpace(equipment) ? null : equipment,
                VideoUrl = string.IsNullOrWhiteSpace(video) ? null : video });
        }

        if (pending.Count == 0) return 0;
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        db.Exercises.AddRange(pending);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return pending.Count;
    }

    private static List<string> Parse(string line)
    {
        var result = new List<string>(); var cell = new System.Text.StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; }
            else if (c == '"') quoted = !quoted;
            else if (c == ',' && !quoted) { result.Add(cell.ToString()); cell.Clear(); }
            else cell.Append(c);
        }
        if (quoted) return [];
        result.Add(cell.ToString());
        return result;
    }
}
