using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Services;
using ImmoDigger.Domain.Entities;
using System.Text.Json;

namespace ImmoDigger.Application.Mapping;

public static class ListingMappingExtensions
{
    private static readonly InvestmentAnalysisService Analysis = new();

    public static ListingSummaryDto ToSummaryDto(this PropertyListing listing) => new(
        listing.Id,
        listing.Source,
        listing.Title,
        listing.Url,
        listing.ImageUrl,
        listing.Description,
        listing.Address,
        listing.City,
        listing.PostalCode,
        listing.AskingPrice,
        listing.CurrentBid,
        listing.ObservedUnitCount ?? listing.OfficialUnitCount,
        listing.BedroomCount,
        listing.BathroomCount,
        DeserializeDocuments(listing.OfficialDocumentsJson).Count,
        listing.LivingArea,
        listing.LandArea,
        listing.PebRating,
        listing.EstimatedGrossYield,
        listing.OpportunityScore,
        listing.RiskLevel,
        listing.RiskSummary,
        listing.SaleType,
        listing.PropertyType,
        listing.IsActive,
        listing.ExternalId.StartsWith("DEMO-", StringComparison.OrdinalIgnoreCase),
        listing.IsReviewed,
        listing.FirstSeenAt,
        listing.UrbanisticStatus,
        listing.ListedSince(),
        // How many of the six criteria the index rests on: an index built
        // from two of them must not read like one built from six.
        listing.OpportunityScore.HasValue
            ? Analysis.CalculateOpportunityScore(listing).EvaluatedCriteriaCount
            : null);

    public static ListingDetailDto ToDetailDto(this PropertyListing listing) => new(
        listing.Id,
        listing.Source,
        listing.ExternalId,
        listing.Url,
        listing.Title,
        listing.ImageUrl,
        listing.Description,
        listing.Address,
        listing.PostalCode,
        listing.City,
        listing.AskingPrice,
        listing.CurrentBid,
        listing.EstimatedFinalPrice,
        listing.SaleType,
        listing.PropertyType,
        listing.BedroomCount,
        listing.BathroomCount,
        listing.OfficialUnitCount,
        listing.OfficialUnitCountSourceName,
        listing.OfficialUnitCountSourceUrl,
        listing.ObservedUnitCount,
        DeserializeDocuments(listing.OfficialDocumentsJson),
        listing.LivingArea,
        listing.LandArea,
        listing.PebRating,
        listing.PebConsumption,
        listing.ElectricalInstallationCompliant,
        listing.IsOccupied,
        listing.HasGarage,
        listing.HasTerrace,
        listing.HasGarden,
        listing.CadastralIncome,
        listing.AuctionStartDate,
        listing.AuctionEndDate,
        listing.FirstSeenAt,
        listing.LastSeenAt,
        listing.PublishedAt,
        listing.IsActive,
        listing.ExternalId.StartsWith("DEMO-", StringComparison.OrdinalIgnoreCase),
        listing.OpportunityScore,
        listing.EstimatedGrossYield,
        listing.EstimatedRenovationCost,
        listing.RiskLevel,
        listing.RiskSummary,
        listing.EstimatedMonthlyRentPerUnit,
        listing.EstimatedAcquisitionCosts,
        listing.EstimatedRenovationBudget,
        listing.PersonalNotes,
        listing.IsReviewed,
        listing.ReviewedAt,
        listing.UrbanisticStatus,
        listing.ListedSince());

    /// <summary>
    /// Earliest date the listing is known to have been on the market: the
    /// source's own publication date when it is older than our first sighting.
    /// </summary>
    public static DateTime ListedSince(this PropertyListing listing) =>
        listing.PublishedAt is { } publishedAt && publishedAt < listing.FirstSeenAt
            ? publishedAt
            : listing.FirstSeenAt;

    private static IReadOnlyCollection<ListingDocumentDto> DeserializeDocuments(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ListingDocumentDto>>(json) ?? [];
        }
        catch (JsonException)
        {
            // A malformed legacy value must not make the whole detail page fail.
            return [];
        }
    }

    public static PriceHistoryEntryDto ToDto(this ListingPriceHistory entry) => new(
        entry.Id, entry.Price, entry.RecordedAt);
}
