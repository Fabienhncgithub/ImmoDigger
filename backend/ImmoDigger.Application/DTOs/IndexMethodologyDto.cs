namespace ImmoDigger.Application.DTOs;

/// <summary>
/// How the ImmoDigger index is computed, generated from the very scales the
/// analysis service scores with - so the explanation shown in the app can
/// never drift from the calculation.
/// </summary>
public sealed record IndexMethodologyDto(
    decimal TotalPoints,
    decimal MinimumAvailablePoints,
    decimal StrongOpportunityThreshold,
    IReadOnlyList<IndexCriterionDto> Criteria);

public sealed record IndexCriterionDto(
    string Key,
    string Label,
    decimal MaxPoints,
    string Basis,
    string CountedWhen,
    IReadOnlyList<IndexStepDto> Steps);

public sealed record IndexStepDto(string Condition, decimal Points);
