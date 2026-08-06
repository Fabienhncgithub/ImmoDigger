using ImmoDigger.Domain.Common;
using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence.DemoData;

/// <summary>
/// Seeds ten fictional <see cref="PropertyListing"/> records so ImmoDigger
/// can be explored end-to-end without calling any real source. Only runs
/// when "DemoMode" is enabled in configuration and the database has no
/// listings yet.
///
/// All addresses, prices and descriptions below are invented for testing
/// purposes; none of them reproduce content from a real listing. The
/// "Avenue Coghen" listing intentionally mirrors the risk-heavy example
/// from the product brief (public sale, non-compliant electrics, PEB G,
/// unit-count mismatch) so the risk analysis service (added in a later
/// commit) has a meaningful fixture to work against.
///
/// ImageUrl values point to Lorem Picsum (picsum.photos), a free stock-photo
/// placeholder service, seeded deterministically per listing - not photos
/// from any real listing.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(ImmoDiggerDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.PropertyListings.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var listings = BuildDemoListings(now);

        dbContext.PropertyListings.AddRange(listings);

        if (!await dbContext.SearchProfiles.AnyAsync(cancellationToken))
        {
            dbContext.SearchProfiles.Add(BuildDemoSearchProfile());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<PropertyListing> BuildDemoListings(DateTime now)
    {
        // 1. Avenue Coghen, Uccle - the reference risk scenario from the
        //    product brief: public sale, unit-count mismatch, PEB G,
        //    non-compliant electrics, occupied by the seller.
        var coghen = new PropertyListing
        {
            Source = "Biddit",
            ExternalId = "DEMO-BIDDIT-001",
            ImageUrl = "https://picsum.photos/seed/DEMO-BIDDIT-001/640/420",
            Url = "https://example-demo.invalid/biddit/demo-001",
            Title = "Maison de rapport - Avenue Coghen",
            Description = "Immeuble de rapport (donnees fictives de demonstration) situe avenue Coghen a Uccle. " +
                           "Plusieurs cuisines sont mentionnees dans le descriptif sans que le nombre exact de " +
                           "logements reconnus ne soit confirme.",
            Address = "Avenue Coghen",
            PostalCode = "1180",
            City = "Uccle",
            AskingPrice = 400_000m,
            SaleType = "PublicSale",
            PropertyType = "IncomeBuilding",
            BedroomCount = 3,
            BathroomCount = 4,
            OfficialUnitCount = 3,
            ObservedUnitCount = 4,
            LivingArea = 320m,
            PebRating = "G",
            ElectricalInstallationCompliant = false,
            IsOccupied = true,
            HasGarage = true,
            HasTerrace = false,
            HasGarden = false,
            FirstSeenAt = now.AddDays(-2),
            LastSeenAt = now,
            PublishedAt = now.AddDays(-2),
            IsActive = true,
            RawContentHash = "demo-hash-biddit-001",
            RiskLevel = RiskLevel.High,
            RiskSummary = "Risque eleve : quatre cuisines sont decrites, mais seulement trois logements " +
                          "semblent officiellement reconnus. Installation electrique non conforme. Vente publique.",
        };
        coghen.PriceHistory.Add(new ListingPriceHistory { Price = 400_000m, RecordedAt = now.AddDays(-2) });

        // 2. Forest - healthy, low-risk apartment building (Immoweb).
        var forest = new PropertyListing
        {
            Source = "Immoweb",
            ExternalId = "DEMO-IMMOWEB-002",
            ImageUrl = "https://picsum.photos/seed/DEMO-IMMOWEB-002/640/420",
            Url = "https://example-demo.invalid/immoweb/demo-002",
            Title = "Immeuble a appartements - Chaussee de Bruxelles",
            Description = "Immeuble de six appartements (donnees fictives) recemment renove, chaussee de " +
                           "Bruxelles a Forest.",
            Address = "Chaussee de Bruxelles",
            PostalCode = "1190",
            City = "Forest",
            AskingPrice = 890_000m,
            SaleType = "RegularSale",
            PropertyType = "ApartmentBuilding",
            OfficialUnitCount = 6,
            ObservedUnitCount = 6,
            LivingArea = 540m,
            PebRating = "C",
            ElectricalInstallationCompliant = true,
            IsOccupied = false,
            HasGarage = false,
            HasTerrace = true,
            HasGarden = false,
            FirstSeenAt = now.AddDays(-5),
            LastSeenAt = now,
            PublishedAt = now.AddDays(-5),
            IsActive = true,
            RawContentHash = "demo-hash-immoweb-002",
            RiskLevel = RiskLevel.Low,
            RiskSummary = "Aucun signal de risque majeur detecte sur les donnees disponibles.",
        };
        forest.PriceHistory.Add(new ListingPriceHistory { Price = 890_000m, RecordedAt = now.AddDays(-5) });

        // 3. Saint-Gilles - moderate risk, missing PEB info (Immovlan).
        var saintGilles = new PropertyListing
        {
            Source = "Immovlan",
            ExternalId = "DEMO-IMMOVLAN-003",
            ImageUrl = "https://picsum.photos/seed/DEMO-IMMOVLAN-003/640/420",
            Url = "https://example-demo.invalid/immovlan/demo-003",
            Title = "Maison de rapport - Rue de la Source",
            Description = "Maison divisee en trois logements (donnees fictives), rue de la Source a Saint-Gilles.",
            Address = "Rue de la Source",
            PostalCode = "1060",
            City = "Saint-Gilles",
            AskingPrice = 520_000m,
            SaleType = "RegularSale",
            PropertyType = "IncomeBuilding",
            OfficialUnitCount = 3,
            ObservedUnitCount = 3,
            LivingArea = 280m,
            PebRating = "E",
            ElectricalInstallationCompliant = null,
            IsOccupied = false,
            HasGarage = false,
            HasTerrace = false,
            HasGarden = true,
            FirstSeenAt = now.AddDays(-9),
            LastSeenAt = now,
            PublishedAt = now.AddDays(-9),
            IsActive = true,
            RawContentHash = "demo-hash-immovlan-003",
            RiskLevel = RiskLevel.Medium,
            RiskSummary = "Conformite de l'installation electrique non precisee dans l'annonce.",
        };
        saintGilles.PriceHistory.Add(new ListingPriceHistory { Price = 520_000m, RecordedAt = now.AddDays(-9) });

        // 4. Ixelles - low risk, good energy rating (Zimmo).
        var ixelles = new PropertyListing
        {
            Source = "Zimmo",
            ExternalId = "DEMO-ZIMMO-004",
            ImageUrl = "https://picsum.photos/seed/DEMO-ZIMMO-004/640/420",
            Url = "https://example-demo.invalid/zimmo/demo-004",
            Title = "Immeuble a appartements - Rue du Bailli",
            Description = "Quatre appartements avec terrasse (donnees fictives), rue du Bailli a Ixelles.",
            Address = "Rue du Bailli",
            PostalCode = "1050",
            City = "Ixelles",
            AskingPrice = 975_000m,
            SaleType = "RegularSale",
            PropertyType = "ApartmentBuilding",
            OfficialUnitCount = 4,
            ObservedUnitCount = 4,
            LivingArea = 410m,
            PebRating = "D",
            ElectricalInstallationCompliant = true,
            IsOccupied = false,
            HasGarage = true,
            HasTerrace = true,
            HasGarden = false,
            FirstSeenAt = now.AddDays(-12),
            LastSeenAt = now,
            PublishedAt = now.AddDays(-12),
            IsActive = true,
            RawContentHash = "demo-hash-zimmo-004",
            RiskLevel = RiskLevel.Low,
            RiskSummary = "Aucun signal de risque majeur detecte sur les donnees disponibles.",
        };
        ixelles.PriceHistory.Add(new ListingPriceHistory { Price = 975_000m, RecordedAt = now.AddDays(-12) });

        // 5. Anderlecht - generic agency source.
        var anderlecht = new PropertyListing
        {
            Source = "GenericAgency",
            ExternalId = "DEMO-AGENCY-005",
            ImageUrl = "https://picsum.photos/seed/DEMO-AGENCY-005/640/420",
            Url = "https://example-demo.invalid/agency/demo-005",
            Title = "Immeuble de rapport - Rue Wayez",
            Description = "Immeuble de cinq logements (donnees fictives), rue Wayez a Anderlecht.",
            Address = "Rue Wayez",
            PostalCode = "1070",
            City = "Anderlecht",
            AskingPrice = 610_000m,
            SaleType = "RegularSale",
            PropertyType = "IncomeBuilding",
            OfficialUnitCount = 5,
            ObservedUnitCount = 5,
            LivingArea = 460m,
            PebRating = "D",
            ElectricalInstallationCompliant = true,
            IsOccupied = false,
            HasGarage = false,
            HasTerrace = false,
            HasGarden = false,
            FirstSeenAt = now.AddDays(-15),
            LastSeenAt = now,
            PublishedAt = now.AddDays(-15),
            IsActive = true,
            RawContentHash = "demo-hash-agency-005",
            RiskLevel = RiskLevel.Low,
            RiskSummary = "Aucun signal de risque majeur detecte sur les donnees disponibles.",
        };
        anderlecht.PriceHistory.Add(new ListingPriceHistory { Price = 610_000m, RecordedAt = now.AddDays(-15) });

        // 6. Schaerbeek - urbanistic risk scenario: no urbanistic info
        //    available and an unrecognised extension mentioned in the text.
        var schaerbeek = new PropertyListing
        {
            Source = "Biddit",
            ExternalId = "DEMO-BIDDIT-006",
            ImageUrl = "https://picsum.photos/seed/DEMO-BIDDIT-006/640/420",
            Url = "https://example-demo.invalid/biddit/demo-006",
            Title = "Immeuble de rapport - Chaussee de Haecht",
            Description = "Immeuble avec extension arriere non documentee (donnees fictives), chaussee de " +
                           "Haecht a Schaerbeek. Aucune information urbanistique disponible dans l'annonce.",
            Address = "Chaussee de Haecht",
            PostalCode = "1030",
            City = "Schaerbeek",
            AskingPrice = 460_000m,
            SaleType = "PublicSale",
            PropertyType = "IncomeBuilding",
            OfficialUnitCount = 4,
            ObservedUnitCount = 4,
            LivingArea = 350m,
            PebRating = "F",
            ElectricalInstallationCompliant = false,
            IsOccupied = false,
            HasGarage = false,
            HasTerrace = false,
            HasGarden = false,
            FirstSeenAt = now.AddDays(-3),
            LastSeenAt = now,
            PublishedAt = now.AddDays(-3),
            IsActive = true,
            RawContentHash = "demo-hash-biddit-006",
            RiskLevel = RiskLevel.High,
            RiskSummary = "Risque eleve : aucune information urbanistique disponible et une extension " +
                          "non documentee est mentionnee dans le descriptif. Vente publique.",
        };
        schaerbeek.PriceHistory.Add(new ListingPriceHistory { Price = 460_000m, RecordedAt = now.AddDays(-3) });

        // 7. Molenbeek-Saint-Jean - price-drop scenario: two price points,
        //    the second one lower than the first.
        var molenbeek = new PropertyListing
        {
            Source = "Immoweb",
            ExternalId = "DEMO-IMMOWEB-007",
            ImageUrl = "https://picsum.photos/seed/DEMO-IMMOWEB-007/640/420",
            Url = "https://example-demo.invalid/immoweb/demo-007",
            Title = "Immeuble a appartements - Rue de Ribaucourt",
            Description = "Immeuble de trois appartements (donnees fictives), rue de Ribaucourt a " +
                           "Molenbeek-Saint-Jean. Prix revu a la baisse recemment.",
            Address = "Rue de Ribaucourt",
            PostalCode = "1080",
            City = "Molenbeek-Saint-Jean",
            AskingPrice = 375_000m,
            SaleType = "RegularSale",
            PropertyType = "ApartmentBuilding",
            OfficialUnitCount = 3,
            ObservedUnitCount = 3,
            LivingArea = 260m,
            PebRating = "E",
            ElectricalInstallationCompliant = true,
            IsOccupied = false,
            HasGarage = false,
            HasTerrace = false,
            HasGarden = false,
            FirstSeenAt = now.AddDays(-20),
            LastSeenAt = now,
            PublishedAt = now.AddDays(-20),
            IsActive = true,
            RawContentHash = "demo-hash-immoweb-007",
            RiskLevel = RiskLevel.Medium,
            RiskSummary = "PEB peu performant (E) ; le rendement doit etre revu en tenant compte d'une " +
                          "possible renovation energetique.",
        };
        molenbeek.PriceHistory.Add(new ListingPriceHistory { Price = 410_000m, RecordedAt = now.AddDays(-20) });
        molenbeek.PriceHistory.Add(new ListingPriceHistory { Price = 375_000m, RecordedAt = now.AddDays(-4) });

        // 8. Etterbeek - small, low-risk building with good yield potential.
        var etterbeek = new PropertyListing
        {
            Source = "Immovlan",
            ExternalId = "DEMO-IMMOVLAN-008",
            ImageUrl = "https://picsum.photos/seed/DEMO-IMMOVLAN-008/640/420",
            Url = "https://example-demo.invalid/immovlan/demo-008",
            Title = "Immeuble de rapport - Rue des Tongres",
            Description = "Petit immeuble de deux logements (donnees fictives), rue des Tongres a Etterbeek.",
            Address = "Rue des Tongres",
            PostalCode = "1040",
            City = "Etterbeek",
            AskingPrice = 340_000m,
            SaleType = "RegularSale",
            PropertyType = "IncomeBuilding",
            OfficialUnitCount = 2,
            ObservedUnitCount = 2,
            LivingArea = 190m,
            PebRating = "C",
            ElectricalInstallationCompliant = true,
            IsOccupied = false,
            HasGarage = false,
            HasTerrace = false,
            HasGarden = false,
            FirstSeenAt = now.AddDays(-7),
            LastSeenAt = now,
            PublishedAt = now.AddDays(-7),
            IsActive = true,
            RawContentHash = "demo-hash-immovlan-008",
            RiskLevel = RiskLevel.Low,
            RiskSummary = "Aucun signal de risque majeur detecte sur les donnees disponibles.",
        };
        etterbeek.PriceHistory.Add(new ListingPriceHistory { Price = 340_000m, RecordedAt = now.AddDays(-7) });

        // 9. Woluwe-Saint-Lambert - higher-end, low-risk building (Zimmo).
        var woluwe = new PropertyListing
        {
            Source = "Zimmo",
            ExternalId = "DEMO-ZIMMO-009",
            ImageUrl = "https://picsum.photos/seed/DEMO-ZIMMO-009/640/420",
            Url = "https://example-demo.invalid/zimmo/demo-009",
            Title = "Immeuble a appartements - Avenue Georges Henri",
            Description = "Immeuble de standing avec cinq appartements (donnees fictives), avenue Georges " +
                           "Henri a Woluwe-Saint-Lambert.",
            Address = "Avenue Georges Henri",
            PostalCode = "1200",
            City = "Woluwe-Saint-Lambert",
            AskingPrice = 1_250_000m,
            SaleType = "RegularSale",
            PropertyType = "ApartmentBuilding",
            OfficialUnitCount = 5,
            ObservedUnitCount = 5,
            LivingArea = 520m,
            PebRating = "B",
            ElectricalInstallationCompliant = true,
            IsOccupied = false,
            HasGarage = true,
            HasTerrace = true,
            HasGarden = true,
            FirstSeenAt = now.AddDays(-18),
            LastSeenAt = now,
            PublishedAt = now.AddDays(-18),
            IsActive = true,
            RawContentHash = "demo-hash-zimmo-009",
            RiskLevel = RiskLevel.Low,
            RiskSummary = "Aucun signal de risque majeur detecte sur les donnees disponibles.",
        };
        woluwe.PriceHistory.Add(new ListingPriceHistory { Price = 1_250_000m, RecordedAt = now.AddDays(-18) });

        // 10. Ganshoren - "just discovered" scenario: first seen a few
        //     minutes ago, and the asking price is still missing (a
        //     risk signal on its own: "prix manquant").
        var ganshoren = new PropertyListing
        {
            Source = "GenericAgency",
            ExternalId = "DEMO-AGENCY-010",
            ImageUrl = "https://picsum.photos/seed/DEMO-AGENCY-010/640/420",
            Url = "https://example-demo.invalid/agency/demo-010",
            Title = "Immeuble de rapport - Avenue Charles-Quint",
            Description = "Immeuble de quatre logements (donnees fictives), avenue Charles-Quint a Ganshoren. " +
                           "Prix communique sur demande.",
            Address = "Avenue Charles-Quint",
            PostalCode = "1083",
            City = "Ganshoren",
            AskingPrice = null,
            SaleType = "RegularSale",
            PropertyType = "IncomeBuilding",
            OfficialUnitCount = 4,
            ObservedUnitCount = 4,
            LivingArea = 300m,
            PebRating = "D",
            ElectricalInstallationCompliant = true,
            IsOccupied = false,
            HasGarage = false,
            HasTerrace = false,
            HasGarden = false,
            FirstSeenAt = now.AddMinutes(-10),
            LastSeenAt = now,
            PublishedAt = now.AddMinutes(-10),
            IsActive = true,
            RawContentHash = "demo-hash-agency-010",
            RiskLevel = RiskLevel.Medium,
            RiskSummary = "Prix manquant dans l'annonce : le rendement ne peut pas etre estime avant " +
                          "confirmation du prix demande.",
        };

        return
        [
            coghen, forest, saintGilles, ixelles, anderlecht,
            schaerbeek, molenbeek, etterbeek, woluwe, ganshoren,
        ];
    }

    private static SearchProfile BuildDemoSearchProfile() =>
        new()
        {
            Name = "Immeuble Bruxelles",
            MaximumPrice = 750_000m,
            MinimumGrossYield = 5.5m,
            MinimumUnitCount = 3,
            RequireGarage = false,
            IncludePublicSales = true,
            PostalCodes = ["1180", "1190", "1060", "1050", "1070"],
            PropertyTypes = ["IncomeBuilding", "ApartmentBuilding"],
            MinimumOpportunityScore = 65m,
            IsEnabled = true,
        };
}
