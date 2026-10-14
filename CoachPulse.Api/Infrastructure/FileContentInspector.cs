using System.IO.Compression;

namespace CoachPulse.Api.Infrastructure;

internal sealed record DetectedFileContent(string ContentType, string Extension);

internal static class FileContentInspector
{
    private static readonly HashSet<string> Mp4Brands = new(StringComparer.Ordinal)
    {
        "isom", "iso2", "iso3", "iso4", "iso5", "iso6", "mp41", "mp42", "avc1", "M4V ", "MSNV", "dash", "F4V "
    };

    public static async Task<DetectedFileContent?> DetectAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
            return null;

        var start = stream.Position;
        var header = new byte[16];
        var count = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        stream.Position = start;

        if (count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return new("image/jpeg", ".jpg");

        if (count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return new("image/png", ".png");

        if (count >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
            header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P')
            return new("image/webp", ".webp");

        if (count >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            return new("application/pdf", ".pdf");

        if (count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }))
            return new("application/msword", ".doc");

        if (count >= 4 && header.AsSpan(0, 4).SequenceEqual(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }) && await HasWebmDocTypeAsync(stream, start, cancellationToken))
            return new("video/webm", ".webm");

        if (count >= 12 && header[4] == 'f' && header[5] == 't' && header[6] == 'y' && header[7] == 'p' &&
            Mp4Brands.Contains(System.Text.Encoding.ASCII.GetString(header, 8, 4)))
            return new("video/mp4", ".mp4");

        if (count >= 4 && header[0] == 'P' && header[1] == 'K' && header[2] is 3 or 5 or 7 && header[3] is 4 or 6 or 8)
        {
            try
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                if (archive.GetEntry("[Content_Types].xml") is not null && archive.GetEntry("word/document.xml") is not null)
                    return new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx");
            }
            catch (InvalidDataException)
            {
                return null;
            }
            finally
            {
                stream.Position = start;
            }
        }

        stream.Position = start;
        return null;
    }

    private static async Task<bool> HasWebmDocTypeAsync(Stream stream, long start, CancellationToken cancellationToken)
    {
        stream.Position = start;
        var header = new byte[4096];
        var count = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        stream.Position = start;
        return header.AsSpan(0, count).IndexOf("webm"u8) >= 0;
    }
}
