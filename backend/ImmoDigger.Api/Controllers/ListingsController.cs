using FluentValidation;
using ImmoDigger.Application.Common;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Mapping;
using ImmoDigger.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController(
    IPropertyListingRepository repository,
    IInvestmentAnalysisService analysisService,
    IValidator<UpdateListingRequest> updateValidator) : ControllerBase
{
    /// <summary>Filtered, sorted and paginated listings.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ListingSummaryDto>>> GetListings(
        [FromQuery] ListingQueryParameters query,
        CancellationToken cancellationToken)
    {
        var paged = await repository.GetPagedAsync(query, cancellationToken);

        return Ok(new PagedResult<ListingSummaryDto>(
            paged.Items.Select(l => l.ToSummaryDto()).ToList(),
            paged.TotalCount,
            paged.Page,
            paged.PageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingDetailDto>> GetListing(Guid id, CancellationToken cancellationToken)
    {
        var listing = await repository.GetByIdAsync(id, cancellationToken);
        return listing is null ? NotFound() : Ok(listing.ToDetailDto());
    }

    /// <summary>Recomputes gross yield, risk assessment and opportunity score, and persists them.</summary>
    [HttpPost("{id:guid}/analyze")]
    public async Task<ActionResult<AnalyzeListingResponse>> Analyze(Guid id, CancellationToken cancellationToken)
    {
        var listing = await repository.GetByIdAsync(id, cancellationToken);
        if (listing is null)
        {
            return NotFound();
        }

        var scoreBreakdown = analysisService.CalculateOpportunityScore(listing);
        var riskAssessment = analysisService.AssessRisk(listing);

        analysisService.Analyze(listing);
        repository.Update(listing);
        await repository.SaveChangesAsync(cancellationToken);

        return Ok(new AnalyzeListingResponse(listing.ToDetailDto(), scoreBreakdown, riskAssessment));
    }

    /// <summary>Updates the user-editable fields (personal notes, manual rent/renovation inputs).</summary>
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ListingDetailDto>> UpdateListing(
        Guid id,
        [FromBody] UpdateListingRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return BadRequest(ModelState);
        }

        var listing = await repository.GetByIdAsync(id, cancellationToken);
        if (listing is null)
        {
            return NotFound();
        }

        listing.PersonalNotes = request.PersonalNotes;
        listing.EstimatedMonthlyRentPerUnit = request.EstimatedMonthlyRentPerUnit;
        listing.EstimatedAcquisitionCosts = request.EstimatedAcquisitionCosts;
        listing.EstimatedRenovationBudget = request.EstimatedRenovationBudget;

        repository.Update(listing);
        await repository.SaveChangesAsync(cancellationToken);

        return Ok(listing.ToDetailDto());
    }

    [HttpPost("{id:guid}/mark-reviewed")]
    public async Task<ActionResult<ListingDetailDto>> MarkReviewed(Guid id, CancellationToken cancellationToken)
    {
        var listing = await repository.GetByIdAsync(id, cancellationToken);
        if (listing is null)
        {
            return NotFound();
        }

        listing.IsReviewed = true;
        listing.ReviewedAt = DateTime.UtcNow;

        repository.Update(listing);
        await repository.SaveChangesAsync(cancellationToken);

        return Ok(listing.ToDetailDto());
    }

    [HttpGet("{id:guid}/price-history")]
    public async Task<ActionResult<IReadOnlyList<PriceHistoryEntryDto>>> GetPriceHistory(
        Guid id, CancellationToken cancellationToken)
    {
        var listing = await repository.GetByIdAsync(id, cancellationToken);
        if (listing is null)
        {
            return NotFound();
        }

        var history = listing.PriceHistory
            .OrderBy(h => h.RecordedAt)
            .Select(h => h.ToDto())
            .ToList();

        return Ok(history);
    }
}
