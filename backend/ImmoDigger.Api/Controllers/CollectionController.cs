using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Mapping;
using ImmoDigger.Infrastructure.BackgroundServices;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Api.Controllers;

[ApiController]
[Route("api/collection")]
public class CollectionController(
    IListingSourceRepository sourceRepository,
    IPropertyListingRepository listingRepository,
    ListingCollectionBackgroundService backgroundService) : ControllerBase
{
    /// <summary>
    /// Triggers a collection cycle immediately instead of waiting for the
    /// next tick. Fire-and-forget: real collectors may take a while, so the
    /// request does not block on completion (mirrors the cycle-level lock
    /// already in place - a cycle already running is simply skipped).
    /// </summary>
    [HttpPost("run")]
    public IActionResult Run()
    {
        _ = backgroundService.RunCollectionCycleAsync(CancellationToken.None);
        return Accepted();
    }

    [HttpGet("status")]
    public async Task<ActionResult<CollectionStatusDto>> Status(CancellationToken cancellationToken)
    {
        var sources = await sourceRepository.GetAllAsync(cancellationToken);
        var dtos = new List<SourceDto>();

        foreach (var source in sources)
        {
            var count = await listingRepository.CountBySourceAsync(source.Name, cancellationToken);
            dtos.Add(source.ToDto(count));
        }

        var lastRunAt = sources
            .Where(s => s.LastSuccessfulRunAt.HasValue)
            .Select(s => s.LastSuccessfulRunAt!.Value)
            .DefaultIfEmpty()
            .Max();

        return Ok(new CollectionStatusDto(dtos, lastRunAt == default ? null : lastRunAt));
    }
}
