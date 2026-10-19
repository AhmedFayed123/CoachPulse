namespace CoachPulse.Application.DTOs.Account;

public sealed record DataExportAcceptedResponse(
    Guid Id,
    string Status,
    string DownloadToken,
    string? StatusUrl,
    string? DownloadUrl);
