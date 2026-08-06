namespace ImmoDigger.Application.DTOs;

public sealed record SourceDto(
    Guid Id,
    string Name,
    string BaseUrl,
    bool IsEnabled,
    int PollingIntervalMinutes,
    DateTime? LastSuccessfulRunAt,
    DateTime? LastFailedRunAt,
    string? LastError,
    int ListingCount);

/// <summary>Body for <c>PATCH /api/sources/{id}</c>: only activation and cadence are user-editable.</summary>
public sealed record UpdateSourceRequest(bool IsEnabled, int? PollingIntervalMinutes);
