using ImmoDigger.Domain.Common;

namespace ImmoDigger.Application.DTOs;

public sealed record SourceDto(
    Guid Id,
    string Name,
    string BaseUrl,
    bool IsEnabled,
    /// <summary>
    /// The hard compliance gate, independent of <see cref="IsEnabled"/> -
    /// see <see cref="Domain.Entities.ListingSource.Allowed"/>. Surfaced so
    /// the UI can explain why an "Activee" source never actually collects
    /// anything, instead of leaving that invisible.
    /// </summary>
    bool Allowed,
    CollectionMethod CollectionMethod,
    string? Notes,
    int PollingIntervalMinutes,
    DateTime? LastSuccessfulRunAt,
    DateTime? LastFailedRunAt,
    string? LastError,
    int ListingCount);

/// <summary>Body for <c>PATCH /api/sources/{id}</c>: only activation and cadence are user-editable.</summary>
public sealed record UpdateSourceRequest(bool IsEnabled, int? PollingIntervalMinutes);
