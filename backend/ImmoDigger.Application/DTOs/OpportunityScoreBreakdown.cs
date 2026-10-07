namespace ImmoDigger.Application.DTOs;

/// <summary>
/// Transparent breakdown of the ImmoDigger index. Missing data is excluded
/// instead of being scored as zero. The raw points are normalised to 100 only
/// when at least 40% of the weighted inputs are available.
/// </summary>
public sealed record OpportunityScoreBreakdown(
    decimal? TotalScore,
    decimal RawScore,
    decimal AvailablePoints,
    decimal DataCompletenessPercentage,
    int EvaluatedCriteriaCount,
    int TotalCriteriaCount,
    decimal PricePerSquareMeterScore,
    bool PricePerSquareMeterAvailable,
    decimal GrossYieldScore,
    bool GrossYieldAvailable,
    decimal UnitCountScore,
    bool UnitCountAvailable,
    decimal LocationScore,
    bool LocationAvailable,
    decimal EnergyScore,
    bool EnergyAvailable,
    decimal RiskScore,
    bool RiskAvailable,
    IReadOnlyCollection<string> MissingData,
    IReadOnlyCollection<string> PositiveSignals,
    IReadOnlyCollection<string> RiskSignals);
