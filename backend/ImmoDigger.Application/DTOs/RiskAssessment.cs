namespace ImmoDigger.Application.DTOs;

/// <summary>
/// Result of scanning a listing for risk signals: an overall level
/// (<see cref="ImmoDigger.Domain.Common.RiskLevel"/>: Low/Medium/High -
/// the highest severity among <see cref="Signals"/> wins), a human-readable
/// summary sentence, and the individual detected signals it was built from.
/// </summary>
public sealed record RiskAssessment(
    string RiskLevel,
    string RiskSummary,
    IReadOnlyCollection<string> Signals);
