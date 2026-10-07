namespace ImmoDigger.Application.DTOs;

/// <summary>Raw aggregate counts behind the dashboard, computed in SQL by the repository.</summary>
public sealed record DashboardStats(
    int NewListingsToday,
    int ActiveListingsCount,
    int RealActiveListingsCount,
    int DemoActiveListingsCount,
    int PricedActiveListingsCount,
    int ScoredActiveListingsCount,
    decimal? AveragePrice,
    decimal? AverageScore,
    int StrongOpportunitiesCount,
    int HighRiskCount);

/// <summary>Shape returned by <c>GET /api/dashboard/summary</c>.</summary>
public sealed record DashboardSummaryDto(
    int NewListingsToday,
    int ActiveListingsCount,
    int RealActiveListingsCount,
    int DemoActiveListingsCount,
    int PricedActiveListingsCount,
    int ScoredActiveListingsCount,
    decimal? AveragePrice,
    decimal? AverageScore,
    int StrongOpportunitiesCount,
    int HighRiskCount,
    IReadOnlyList<ListingSummaryDto> RecentListings);
