namespace CoachPulse.Application.DTOs.Account;

public sealed record DataExportStatusResponse(
    Guid Id,
    string Status,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? DownloadedAt,
    string? FailureCode);
