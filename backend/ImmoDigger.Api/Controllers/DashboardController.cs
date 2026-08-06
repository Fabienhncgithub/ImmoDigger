using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IPropertyListingRepository repository) : ControllerBase
{
    // V1 placeholder threshold for "strong opportunity" - not derived from
    // real portfolio data, easy to revisit once there is a track record.
    private const decimal StrongOpportunityThreshold = 70m;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var stats = await repository.GetDashboardStatsAsync(StrongOpportunityThreshold, cancellationToken);
        var recent = await repository.GetPagedAsync(new ListingQueryParameters { PageSize = 5 }, cancellationToken);

        return Ok(new DashboardSummaryDto(
            stats.NewListingsToday,
            stats.ActiveListingsCount,
            stats.AveragePrice,
            stats.AverageScore,
            stats.StrongOpportunitiesCount,
            stats.HighRiskCount,
            recent.Items.Select(l => l.ToSummaryDto()).ToList()));
    }
}
