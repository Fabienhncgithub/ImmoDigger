using ImmoDigger.Application.DTOs;
using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Mapping;

public static class ListingMappingExtensions
{
    public static ListingSummaryDto ToSummaryDto(this PropertyListing listing) => new(
        listing.Id,
        listing.Source,
        listing.Title,
        listing.ImageUrl,
        listing.City,
        listing.PostalCode,
        listing.AskingPrice,
        listing.CurrentBid,
        listing.ObservedUnitCount ?? listing.OfficialUnitCount,
        listing.LivingArea,
        listing.PebRating,
        listing.EstimatedGrossYield,
        listing.OpportunityScore,
        listing.RiskLevel,
        listing.SaleType,
        listing.PropertyType,
        listing.IsActive,
        listing.IsReviewed,
        listing.FirstSeenAt);

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
        listing.ObservedUnitCount,
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
        listing.ReviewedAt);

    public static PriceHistoryEntryDto ToDto(this ListingPriceHistory entry) => new(
        entry.Id, entry.Price, entry.RecordedAt);
}
