namespace ImmoDigger.Application.DTOs;

public sealed record PriceHistoryEntryDto(Guid Id, decimal Price, DateTime RecordedAt);
