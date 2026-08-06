namespace ImmoDigger.Application.DTOs;

/// <summary>
/// Transparent breakdown of a listing's opportunity score (0-100):
/// price per square meter (25 pts), estimated gross yield (25 pts), unit
/// count (15 pts), location quality (15 pts), energy rating (10 pts), and
/// legal/urbanistic risk (10 pts).
/// </summary>
public sealed record OpportunityScoreBreakdown(
    decimal TotalScore,
    decimal PricePerSquareMeterScore,
    decimal GrossYieldScore,
    decimal UnitCountScore,
    decimal LocationScore,
    decimal EnergyScore,
    decimal RiskScore,
    IReadOnlyCollection<string> PositiveSignals,
    IReadOnlyCollection<string> RiskSignals);
