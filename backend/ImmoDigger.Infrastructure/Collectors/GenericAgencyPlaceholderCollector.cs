using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;

namespace ImmoDigger.Infrastructure.Collectors;

/// <summary>
/// Documented placeholder for the "generic real-estate agency" source.
///
/// No real agency site has been selected and vetted yet: before pointing
/// this collector at an actual site, we must confirm it exposes a public
/// API/RSS feed or otherwise allows polite HTML collection under its terms
/// of use, per the project's collection rules (no bypassing CAPTCHAs,
/// auth walls, rate limits, or explicit prohibitions).
///
/// Until that diligence is done, this implementation returns a couple of
/// fixed, clearly-fictional listings. It exists to exercise the collection
/// framework (background service, run-history tracking, error handling)
/// end to end, and matches the "GenericAgency" <see cref="Domain.Entities.ListingSource"/>
/// row, which is seeded disabled by default.
/// </summary>
public class GenericAgencyPlaceholderCollector : IListingCollector
{
    public string SourceName => "GenericAgency";

    public Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CollectedListing> listings =
        [
            new CollectedListing
            {
                Source = SourceName,
                ExternalId = "PLACEHOLDER-001",
                Url = "https://example-agency.invalid/listing/placeholder-001",
                Title = "Immeuble de rapport (donnees fictives - collecteur non implemente)",
                Description = "Ce collecteur est un placeholder documente : aucune source d'agence reelle " +
                               "n'a encore ete validee. Ne pas utiliser ces donnees comme references reelles.",
                Address = "Rue Fictive 1",
                PostalCode = "1000",
                City = "Bruxelles",
                AskingPrice = 500_000m,
                SaleType = "RegularSale",
                PropertyType = "IncomeBuilding",
                OfficialUnitCount = 3,
                ObservedUnitCount = 3,
                RawContentHash = "placeholder-hash-001",
            },
        ];

        return Task.FromResult(listings);
    }
}
