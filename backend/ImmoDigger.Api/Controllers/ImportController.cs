using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Api.Controllers;

/// <summary>Manual, one-listing-at-a-time import (Source 4 of the compliant multi-source pipeline).</summary>
[ApiController]
[Route("api/import")]
public class ImportController(
    IManualListingImportService importService,
    IListingDeduplicationService deduplicationService,
    IPropertyListingRepository repository) : ControllerBase
{
    /// <summary>
    /// Imports the listing at <paramref name="request"/>'s URL. If the page
    /// can't be fetched or has no usable metadata, responds 422 with
    /// <c>requiresManualFallback: true</c> - resubmit with
    /// <see cref="ImportUrlRequest.ManualFallback"/> filled in.
    /// </summary>
    [HttpPost("url")]
    public async Task<ActionResult<ListingDetailDto>> ImportFromUrl(
        [FromBody] ImportUrlRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest("Url is required.");
        }

        var result = await importService.ImportFromUrlAsync(request, cancellationToken);

        if (result.Listing is null)
        {
            return UnprocessableEntity(new { requiresManualFallback = true });
        }

        var outcome = await deduplicationService.ProcessAsync(result.Listing, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return outcome.Listing is null
            ? UnprocessableEntity(new { requiresManualFallback = false, reasons = outcome.Reasons })
            : Ok(outcome.Listing.ToDetailDto());
    }
}
