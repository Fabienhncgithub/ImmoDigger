namespace ImmoDigger.Application.DTOs;

/// <summary>
/// Result of <c>POST /api/listings/{id}/analyze</c>: the updated listing
/// plus the transparent breakdowns the score and risk level were computed
/// from - not just the persisted scalar values.
/// </summary>
public sealed record AnalyzeListingResponse(
    ListingDetailDto Listing,
    OpportunityScoreBreakdown ScoreBreakdown,
    RiskAssessment RiskAssessment);
