using ImmoDigger.Application.DTOs;
using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Mapping;

public static class SourceMappingExtensions
{
    public static SourceDto ToDto(this ListingSource source, int listingCount) => new(
        source.Id,
        source.Name,
        source.BaseUrl,
        source.IsEnabled,
        source.Allowed,
        source.CollectionMethod,
        source.Notes,
        source.PollingIntervalMinutes,
        source.LastSuccessfulRunAt,
        source.LastFailedRunAt,
        source.LastError,
        listingCount);
}
