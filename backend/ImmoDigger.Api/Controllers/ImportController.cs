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
    IInvestmentAnalysisService analysisService,
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
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest("A valid absolute HTTP or HTTPS URL is required.");
        }

        if (request.Url.Length > 2000)
        {
            return BadRequest("URL must not exceed 2000 characters.");
        }

        if (request.ManualFallback is { } manual)
        {
            if (string.IsNullOrWhiteSpace(manual.Title))
            {
                return BadRequest("Manual fallback title is required.");
            }

            if (manual.Title.Length > 500 || manual.Address?.Length > 300 ||
                manual.PostalCode?.Length > 20 || manual.City?.Length > 150 ||
                manual.ImageUrl?.Length > 2000)
            {
                return BadRequest("One or more manual fallback fields exceed their maximum length.");
            }

            if (manual.Price is < 0)
            {
                return BadRequest("Price cannot be negative.");
            }
        }

        var result = await importService.ImportFromUrlAsync(request, cancellationToken);

        if (result.Listing is null)
        {
            return UnprocessableEntity(new { requiresManualFallback = true });
        }

        var outcome = await deduplicationService.ProcessAsync(result.Listing, cancellationToken);
        if (outcome.Listing is not null)
        {
            // Manual imports must be immediately usable like collected
            // listings: calculate yield/risk/score before returning them.
            analysisService.Analyze(outcome.Listing);
        }

        await repository.SaveChangesAsync(cancellationToken);

        return outcome.Listing is null
            ? UnprocessableEntity(new { requiresManualFallback = false, reasons = outcome.Reasons })
            : Ok(outcome.Listing.ToDetailDto());
    }
}
