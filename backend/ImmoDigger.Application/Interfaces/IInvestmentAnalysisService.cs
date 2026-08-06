using ImmoDigger.Application.DTOs;
using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Interfaces;

/// <summary>
/// Computes investment indicators for a listing: estimated gross rental
/// yield, a risk assessment (Low/Medium/High with an explanation), and a
/// transparent opportunity score out of 100. Classic, readable business
/// rules - no external AI model.
/// </summary>
public interface IInvestmentAnalysisService
{
    /// <summary>
    /// Gross yield (%) = annual estimated rent / total acquisition cost,
    /// using the listing's manually entered
    /// <see cref="PropertyListing.EstimatedMonthlyRentPerUnit"/>,
    /// <see cref="PropertyListing.EstimatedAcquisitionCosts"/> and
    /// <see cref="PropertyListing.EstimatedRenovationBudget"/>.
    /// Returns <see langword="null"/> when the inputs needed to compute it
    /// (rent estimate, asking price, unit count) are missing.
    /// </summary>
    decimal? EstimateGrossYield(PropertyListing listing);

    RiskAssessment AssessRisk(PropertyListing listing);

    OpportunityScoreBreakdown CalculateOpportunityScore(PropertyListing listing);

    /// <summary>
    /// Runs all three analyses and writes their results onto the listing
    /// (<see cref="PropertyListing.EstimatedGrossYield"/>,
    /// <see cref="PropertyListing.RiskLevel"/>,
    /// <see cref="PropertyListing.RiskSummary"/>,
    /// <see cref="PropertyListing.OpportunityScore"/>). Does not save;
    /// the caller controls persistence.
    /// </summary>
    void Analyze(PropertyListing listing);
}
