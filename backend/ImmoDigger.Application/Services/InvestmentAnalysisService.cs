using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Entities;
using RiskLevel = ImmoDigger.Domain.Common.RiskLevel;

namespace ImmoDigger.Application.Services;

/// <inheritdoc cref="IInvestmentAnalysisService" />
public class InvestmentAnalysisService : IInvestmentAnalysisService
{
    // V1 placeholder heuristic: a simple three-tier classification of a
    // handful of Brussels-region postal codes, not real market data (price
    // indices, transit access, ...). Meant to be swapped out for a real
    // data source later; kept here so it stays visible and easy to change.
    private static readonly Dictionary<string, decimal> LocationTierScores = new()
    {
        // Tier A - 15 pts
        ["1000"] = 15m, // Bruxelles-Ville
        ["1050"] = 15m, // Ixelles
        ["1180"] = 15m, // Uccle
        ["1200"] = 15m, // Woluwe-Saint-Lambert
        // Tier B - 10 pts
        ["1060"] = 10m, // Saint-Gilles
        ["1190"] = 10m, // Forest
        ["1040"] = 10m, // Etterbeek
        ["1170"] = 10m, // Watermael-Boitsfort
        // Tier C - 6 pts
        ["1070"] = 6m, // Anderlecht
        ["1030"] = 6m, // Schaerbeek
        ["1080"] = 6m, // Molenbeek-Saint-Jean
        ["1083"] = 6m, // Ganshoren
    };

    private const decimal DefaultLocationScore = 5m; // neutral default for an unlisted postal code

    private static readonly string[] UrbanisticKeywords =
    [
        "urbanistique", "urbanisme", "permis d'urbanisme", "infraction urbanistique", "affectation",
    ];

    public decimal? EstimateGrossYield(PropertyListing listing)
    {
        if (listing.EstimatedMonthlyRentPerUnit is not (> 0) || listing.AskingPrice is not (> 0))
        {
            return null;
        }

        var unitCount = listing.ObservedUnitCount ?? listing.OfficialUnitCount;
        if (unitCount is not (> 0))
        {
            return null;
        }

        var annualRent = listing.EstimatedMonthlyRentPerUnit.Value * 12 * unitCount.Value;
        var acquisitionCost = listing.AskingPrice.Value
            + (listing.EstimatedAcquisitionCosts ?? 0)
            + (listing.EstimatedRenovationBudget ?? 0);

        if (acquisitionCost <= 0)
        {
            return null;
        }

        return Math.Round(annualRent / acquisitionCost * 100m, 2);
    }

    public RiskAssessment AssessRisk(PropertyListing listing)
    {
        var highSeverity = new List<string>();
        var mediumSeverity = new List<string>();

        if (listing.PebRating is "F" or "G")
        {
            highSeverity.Add($"Performance energetique tres faible (PEB {listing.PebRating}).");
        }

        if (listing.ElectricalInstallationCompliant == false)
        {
            highSeverity.Add("Installation electrique non conforme.");
        }

        var isPublicSale = string.Equals(listing.SaleType, "PublicSale", StringComparison.OrdinalIgnoreCase);

        var hasUnrecognizedUnits = listing.ObservedUnitCount.HasValue && listing.OfficialUnitCount.HasValue &&
                                    listing.ObservedUnitCount.Value > listing.OfficialUnitCount.Value;
        if (hasUnrecognizedUnits)
        {
            highSeverity.Add(
                $"{listing.ObservedUnitCount} logement(s) semblent decrits dans l'annonce, mais seulement " +
                $"{listing.OfficialUnitCount} sont officiellement reconnus - logement(s) potentiellement non reconnu(s).");
        }

        var mentionsUrbanisticInfo = !string.IsNullOrWhiteSpace(listing.Description) &&
            UrbanisticKeywords.Any(keyword => listing.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (isPublicSale && !mentionsUrbanisticInfo)
        {
            highSeverity.Add("Vente publique sans information urbanistique mentionnee dans l'annonce.");
        }
        else if (isPublicSale)
        {
            mediumSeverity.Add("Vente publique.");
        }

        if (listing.IsOccupied == true)
        {
            mediumSeverity.Add("Bien occupe.");
        }

        if (listing.AskingPrice is null && listing.CurrentBid is null)
        {
            mediumSeverity.Add("Prix manquant dans l'annonce.");
        }

        if (listing.ObservedUnitCount is > 0 && listing.LivingArea is > 0 &&
            listing.LivingArea.Value / listing.ObservedUnitCount.Value < 15m)
        {
            mediumSeverity.Add("Surface habitable incoherente au regard du nombre de logements annonce.");
        }

        if (listing.BedroomCount.HasValue && listing.ObservedUnitCount.HasValue &&
            listing.BedroomCount.Value < listing.ObservedUnitCount.Value)
        {
            mediumSeverity.Add(
                $"Donnees contradictoires : {listing.BedroomCount} chambre(s) annoncee(s) pour " +
                $"{listing.ObservedUnitCount} logement(s) observe(s).");
        }

        var allSignals = highSeverity.Concat(mediumSeverity).ToList();

        var level = highSeverity.Count > 0
            ? RiskLevel.High
            : mediumSeverity.Count > 0
                ? RiskLevel.Medium
                : RiskLevel.Low;

        var summary = allSignals.Count == 0
            ? "Aucun signal de risque majeur detecte sur les donnees disponibles."
            : $"Risque {TranslateLevel(level)} : {string.Join(" ", allSignals)}";

        return new RiskAssessment(level, summary, allSignals);
    }

    public OpportunityScoreBreakdown CalculateOpportunityScore(PropertyListing listing)
    {
        var positiveSignals = new List<string>();

        var pricePerSqmScore = ScorePricePerSquareMeter(listing, positiveSignals);
        var yieldScore = ScoreGrossYield(listing, positiveSignals);
        var unitCountScore = ScoreUnitCount(listing, positiveSignals);
        var locationScore = ScoreLocation(listing, positiveSignals);
        var energyScore = ScoreEnergy(listing, positiveSignals);

        var riskAssessment = AssessRisk(listing);
        var riskScore = riskAssessment.RiskLevel switch
        {
            RiskLevel.Low => 10m,
            RiskLevel.Medium => 5m,
            _ => 0m,
        };

        var total = pricePerSqmScore + yieldScore + unitCountScore + locationScore + energyScore + riskScore;

        return new OpportunityScoreBreakdown(
            TotalScore: total,
            PricePerSquareMeterScore: pricePerSqmScore,
            GrossYieldScore: yieldScore,
            UnitCountScore: unitCountScore,
            LocationScore: locationScore,
            EnergyScore: energyScore,
            RiskScore: riskScore,
            PositiveSignals: positiveSignals,
            RiskSignals: riskAssessment.Signals);
    }

    public void Analyze(PropertyListing listing)
    {
        listing.EstimatedGrossYield = EstimateGrossYield(listing);

        var risk = AssessRisk(listing);
        listing.RiskLevel = risk.RiskLevel;
        listing.RiskSummary = risk.RiskSummary;

        listing.OpportunityScore = CalculateOpportunityScore(listing).TotalScore;
    }

    private static decimal ScorePricePerSquareMeter(PropertyListing listing, List<string> positiveSignals)
    {
        if (listing.AskingPrice is not (> 0) || listing.LivingArea is not (> 0))
        {
            return 0m;
        }

        var pricePerSqm = listing.AskingPrice.Value / listing.LivingArea.Value;

        var score = pricePerSqm switch
        {
            <= 1500m => 25m,
            <= 2000m => 20m,
            <= 2500m => 15m,
            <= 3000m => 10m,
            <= 3500m => 5m,
            _ => 0m,
        };

        if (score >= 20m)
        {
            positiveSignals.Add($"Prix par m2 attractif (~{pricePerSqm:N0} EUR/m2).");
        }

        return score;
    }

    private decimal ScoreGrossYield(PropertyListing listing, List<string> positiveSignals)
    {
        var grossYield = EstimateGrossYield(listing);
        if (grossYield is null)
        {
            return 0m;
        }

        var score = grossYield.Value switch
        {
            >= 8m => 25m,
            >= 7m => 20m,
            >= 6m => 15m,
            >= 5m => 10m,
            >= 4m => 5m,
            _ => 0m,
        };

        if (score >= 15m)
        {
            positiveSignals.Add($"Rendement brut estime interessant (~{grossYield:N1} %).");
        }

        return score;
    }

    private static decimal ScoreUnitCount(PropertyListing listing, List<string> positiveSignals)
    {
        var unitCount = listing.ObservedUnitCount ?? listing.OfficialUnitCount;
        if (unitCount is not (> 0))
        {
            return 0m;
        }

        var score = unitCount.Value switch
        {
            >= 6 => 15m,
            >= 4 => 11m,
            >= 3 => 8m,
            >= 2 => 5m,
            _ => 0m,
        };

        if (score >= 8m)
        {
            positiveSignals.Add($"{unitCount} logements : bonne mutualisation du risque locatif.");
        }

        return score;
    }

    private static decimal ScoreLocation(PropertyListing listing, List<string> positiveSignals)
    {
        if (string.IsNullOrWhiteSpace(listing.PostalCode) ||
            !LocationTierScores.TryGetValue(listing.PostalCode.Trim(), out var score))
        {
            return DefaultLocationScore;
        }

        if (score >= 15m)
        {
            positiveSignals.Add($"Localisation ({listing.City}) consideree comme premium.");
        }

        return score;
    }

    private static decimal ScoreEnergy(PropertyListing listing, List<string> positiveSignals)
    {
        var score = listing.PebRating switch
        {
            "A" => 10m,
            "B" => 8m,
            "C" => 6m,
            "D" => 4m,
            "E" => 2m,
            "F" => 1m,
            "G" => 0m,
            _ => 3m, // unknown PEB: neutral-ish, slightly penalized for missing data
        };

        if (score >= 8m)
        {
            positiveSignals.Add($"Bonne performance energetique (PEB {listing.PebRating}).");
        }

        return score;
    }

    private static string TranslateLevel(string level) => level switch
    {
        RiskLevel.High => "eleve",
        RiskLevel.Medium => "modere",
        _ => "faible",
    };
}
