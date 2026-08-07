using ImmoDigger.Application.Common;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Common;
using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Services;

/// <inheritdoc cref="IListingDeduplicationService" />
public class ListingDeduplicationService(IPropertyListingRepository repository) : IListingDeduplicationService
{
    private const double TitleSimilarityThreshold = 0.5;
    private const decimal PriceTolerance = 0.02m; // 2%
    private const decimal AreaTolerance = 0.05m; // 5%

    public async Task<DeduplicationOutcome> ProcessAsync(CollectedListing collected, CancellationToken cancellationToken = default)
    {
        // Level 1: exact Source + ExternalId match.
        var existing = await repository.GetBySourceAndExternalIdAsync(collected.Source, collected.ExternalId, cancellationToken);

        // Level 2: exact normalized URL match (catches a listing whose
        // external id changed or was missing on a previous run, as long as
        // its URL is stable).
        existing ??= await repository.GetByNormalizedUrlAsync(UrlNormalizer.Normalize(collected.Url), cancellationToken);

        if (existing is not null)
        {
            return UpdateExisting(existing, collected);
        }

        // Levels 3+4 (normalized address corroborated by title/price/area
        // similarity) and level 5 (identical content hash) never trigger an
        // automatic write; they only flag a probable duplicate for review.
        var candidates = await repository.GetAllAsync(cancellationToken);
        var duplicateReasons = FindProbableDuplicate(collected, candidates);
        if (duplicateReasons is not null)
        {
            return DeduplicationOutcome.Duplicate(duplicateReasons);
        }

        return await CreateNewAsync(collected, cancellationToken);
    }

    private async Task<DeduplicationOutcome> CreateNewAsync(CollectedListing collected, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var listing = new PropertyListing
        {
            Source = collected.Source,
            ExternalId = collected.ExternalId,
            Url = collected.Url,
            Title = collected.Title,
            RawContentHash = collected.RawContentHash,
            SaleType = collected.SaleType,
            PropertyType = collected.PropertyType,
            FirstSeenAt = now,
            LastSeenAt = now,
            IsActive = true,
        };

        ApplyCollectedFields(listing, collected);

        if (collected.AskingPrice.HasValue)
        {
            listing.PriceHistory.Add(new ListingPriceHistory { Price = collected.AskingPrice.Value, RecordedAt = now });
        }

        await repository.AddAsync(listing, cancellationToken);

        return DeduplicationOutcome.New(listing);
    }

    private DeduplicationOutcome UpdateExisting(PropertyListing existing, CollectedListing collected)
    {
        var now = DateTime.UtcNow;
        var reasons = new List<string>();

        var priceChanged = existing.AskingPrice != collected.AskingPrice;
        var contentChanged = !string.Equals(existing.RawContentHash, collected.RawContentHash, StringComparison.Ordinal);

        // A source can reassign a listing's external id over time (seen when
        // matching happened via the normalized URL, level 2); that alone is
        // still worth recording as an update even if nothing else changed.
        var externalIdChanged = !string.Equals(existing.ExternalId, collected.ExternalId, StringComparison.Ordinal);

        if (!priceChanged && !contentChanged && !externalIdChanged)
        {
            existing.LastSeenAt = now;
            existing.IsActive = true;
            return DeduplicationOutcome.Unchanged(existing);
        }

        if (priceChanged)
        {
            reasons.Add(
                $"Prix modifie : {FormatPrice(existing.AskingPrice)} -> {FormatPrice(collected.AskingPrice)}.");

            if (collected.AskingPrice.HasValue)
            {
                repository.AddPriceHistoryEntry(
                    existing, new ListingPriceHistory { Price = collected.AskingPrice.Value, RecordedAt = now });
            }
        }

        if (contentChanged)
        {
            reasons.Add("Contenu de l'annonce modifie.");
        }

        if (externalIdChanged)
        {
            reasons.Add($"Identifiant externe modifie sur la source : {existing.ExternalId} -> {collected.ExternalId}.");
            existing.ExternalId = collected.ExternalId;
        }

        ApplyCollectedFields(existing, collected);
        existing.RawContentHash = collected.RawContentHash;
        existing.LastSeenAt = now;
        existing.IsActive = true;

        return DeduplicationOutcome.Updated(existing, reasons);
    }

    /// <summary>Copies every mutable, collector-sourced field from <paramref name="collected"/> onto <paramref name="listing"/>.</summary>
    private static void ApplyCollectedFields(PropertyListing listing, CollectedListing collected)
    {
        listing.Title = collected.Title;
        listing.ImageUrl = collected.ImageUrl;
        listing.Description = collected.Description;
        listing.Address = collected.Address;
        listing.PostalCode = collected.PostalCode;
        listing.City = collected.City;
        listing.AskingPrice = collected.AskingPrice;
        listing.CurrentBid = collected.CurrentBid;
        listing.SaleType = collected.SaleType;
        listing.PropertyType = collected.PropertyType;
        listing.BedroomCount = collected.BedroomCount;
        listing.BathroomCount = collected.BathroomCount;
        listing.OfficialUnitCount = collected.OfficialUnitCount;
        listing.ObservedUnitCount = collected.ObservedUnitCount;
        listing.LivingArea = collected.LivingArea;
        listing.LandArea = collected.LandArea;
        listing.PebRating = collected.PebRating;
        listing.PebConsumption = collected.PebConsumption;
        listing.ElectricalInstallationCompliant = collected.ElectricalInstallationCompliant;
        listing.IsOccupied = collected.IsOccupied;
        listing.HasGarage = collected.HasGarage;
        listing.HasTerrace = collected.HasTerrace;
        listing.HasGarden = collected.HasGarden;
        listing.CadastralIncome = collected.CadastralIncome;
        listing.AuctionStartDate = collected.AuctionStartDate;
        listing.AuctionEndDate = collected.AuctionEndDate;
        listing.PublishedAt = collected.PublishedAt;
        listing.EmailMessageId = collected.EmailMessageId;
        listing.EmailSubject = collected.EmailSubject;
        listing.EmailSender = collected.EmailSender;
    }

    /// <summary>
    /// Levels 3+4+5 combined: an identical cleaned-content hash (level 5) is
    /// a strong enough signal on its own. Otherwise, a shared normalized
    /// address (level 3) is only treated as a probable duplicate once
    /// corroborated by a similar title, and a close price and living area
    /// (level 4) - a bare address match is too weak on its own (e.g. a
    /// building legitimately re-listed, or sold unit by unit).
    /// </summary>
    private static IReadOnlyCollection<string>? FindProbableDuplicate(
        CollectedListing collected,
        IReadOnlyList<PropertyListing> candidates)
    {
        var hasAddress = !string.IsNullOrWhiteSpace(collected.Address) || !string.IsNullOrWhiteSpace(collected.PostalCode);
        var addressKey = AddressNormalizer.Normalize(collected.Address, collected.PostalCode, collected.City);

        foreach (var candidate in candidates.Where(c => c.IsActive))
        {
            if (!string.IsNullOrWhiteSpace(collected.RawContentHash) &&
                !string.IsNullOrWhiteSpace(candidate.RawContentHash) &&
                string.Equals(candidate.RawContentHash, collected.RawContentHash, StringComparison.Ordinal))
            {
                return [$"Hash de contenu identique a l'annonce {candidate.Source}/{candidate.ExternalId}."];
            }

            if (!hasAddress)
            {
                continue;
            }

            var candidateAddressKey = AddressNormalizer.Normalize(candidate.Address, candidate.PostalCode, candidate.City);
            if (candidateAddressKey != addressKey)
            {
                continue;
            }

            var titleSimilarity = TextSimilarity.WordOverlapRatio(collected.Title, candidate.Title);
            var priceClose = IsClose(collected.AskingPrice, candidate.AskingPrice, PriceTolerance);
            var areaClose = IsClose(collected.LivingArea, candidate.LivingArea, AreaTolerance);

            if (titleSimilarity >= TitleSimilarityThreshold && priceClose && areaClose)
            {
                return [
                    $"Meme adresse que l'annonce {candidate.Source}/{candidate.ExternalId}, " +
                    $"titre similaire ({titleSimilarity:P0}), prix et surface proches."
                ];
            }
        }

        return null;
    }

    private static bool IsClose(decimal? a, decimal? b, decimal tolerance)
    {
        if (a is null || b is null)
        {
            return false;
        }

        if (b == 0)
        {
            return a == 0;
        }

        return Math.Abs(a.Value - b.Value) / Math.Abs(b.Value) <= tolerance;
    }

    private static string FormatPrice(decimal? price) => price.HasValue ? price.Value.ToString("N0") : "inconnu";
}
