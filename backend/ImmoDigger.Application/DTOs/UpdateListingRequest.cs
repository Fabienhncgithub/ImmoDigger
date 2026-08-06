namespace ImmoDigger.Application.DTOs;

/// <summary>
/// Body of <c>PATCH /api/listings/{id}</c>: the user-editable fields shown
/// on the listing detail page (personal notes, manual rent/renovation
/// inputs used by <see cref="Interfaces.IInvestmentAnalysisService"/>).
/// Simplified PATCH semantics: any property present in the JSON body
/// (including an explicit <see langword="null"/>) overwrites the
/// corresponding field - there is no distinct "field omitted" tracking
/// (e.g. RFC 6902 JSON Patch), which is more machinery than a personal-use
/// app needs.
/// </summary>
public sealed record UpdateListingRequest(
    string? PersonalNotes,
    decimal? EstimatedMonthlyRentPerUnit,
    decimal? EstimatedAcquisitionCosts,
    decimal? EstimatedRenovationBudget);
