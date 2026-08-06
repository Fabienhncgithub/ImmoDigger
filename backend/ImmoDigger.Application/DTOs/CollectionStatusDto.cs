namespace ImmoDigger.Application.DTOs;

/// <summary>Shape returned by <c>GET /api/collection/status</c>.</summary>
public sealed record CollectionStatusDto(IReadOnlyList<SourceDto> Sources, DateTime? LastRunAt);
