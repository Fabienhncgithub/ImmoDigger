using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Mapping;
using ImmoDigger.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IPropertyListingRepository repository) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var stats = await repository.GetDashboardStatsAsync(InvestmentAnalysisService.StrongOpportunityThreshold, cancellationToken);
        var recent = await repository.GetPagedAsync(
            new ListingQueryParameters { PageSize = 5, IsActive = true }, cancellationToken);

        return Ok(new DashboardSummaryDto(
            stats.NewListingsToday,
            stats.ActiveListingsCount,
            stats.RealActiveListingsCount,
            stats.DemoActiveListingsCount,
            stats.PricedActiveListingsCount,
            stats.ScoredActiveListingsCount,
            stats.AveragePrice,
            stats.AverageScore,
            stats.StrongOpportunitiesCount,
            stats.HighRiskCount,
            recent.Items.Select(l => l.ToSummaryDto()).ToList()));
    }
}
