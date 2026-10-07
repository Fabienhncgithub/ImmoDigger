using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Entities;
using RiskLevel = ImmoDigger.Domain.Common.RiskLevel;
using UrbanisticStatus = ImmoDigger.Domain.Common.UrbanisticStatus;

namespace ImmoDigger.Application.Services;

/// <inheritdoc cref="IInvestmentAnalysisService" />
public class InvestmentAnalysisService : IInvestmentAnalysisService
{
    /// <summary>Index from which a listing counts as a "strong opportunity" on the dashboard.</summary>
    public const decimal StrongOpportunityThreshold = 70m;

    private const decimal PricePerSquareMeterMaxPoints = 25m;
    private const decimal GrossYieldMaxPoints = 25m;
    private const decimal UnitCountMaxPoints = 15m;
    private const decimal LocationMaxPoints = 15m;
    private const decimal EnergyMaxPoints = 10m;
    private const decimal RiskMaxPoints = 10m;

    // Below this many points' worth of known criteria, no index is shown at all.
    private const decimal MinimumAvailablePointsForIndex = 40m;

    private static readonly System.Globalization.CultureInfo FrenchBelgium =
        System.Globalization.CultureInfo.GetCultureInfo("fr-BE");

    // The scales below are both what the scoring methods apply and what
    // DescribeIndex() publishes to the "how is the index computed" page.

    /// <summary>Price per m² of living area, in EUR: at most <c>Max</c> earns <c>Points</c>.</summary>
    private static readonly (decimal Max, decimal Points)[] PricePerSquareMeterSteps =
        [(1500m, 25m), (2000m, 20m), (2500m, 15m), (3000m, 10m), (3500m, 5m)];

    /// <summary>Estimated gross yield, in %: at least <c>Min</c> earns <c>Points</c>.</summary>
    private static readonly (decimal Min, decimal Points)[] GrossYieldSteps =
        [(8m, 25m), (7m, 20m), (6m, 15m), (5m, 10m), (4m, 5m)];

    /// <summary>Number of housing units: at least <c>Min</c> earns <c>Points</c>.</summary>
    private static readonly (int Min, decimal Points)[] UnitCountSteps =
        [(6, 15m), (4, 11m), (3, 8m), (2, 5m)];

    private static readonly (string Rating, decimal Points)[] EnergySteps =
        [("A", 10m), ("B", 8m), ("C", 6m), ("D", 4m), ("E", 2m), ("F", 1m), ("G", 0m)];

    private static readonly (string Level, string Label, decimal Points)[] RiskSteps =
    [
        (RiskLevel.Low, "Aucune alerte détectée", 10m),
        (RiskLevel.Medium, "Points à vérifier", 5m),
        (RiskLevel.High, "Alerte majeure", 0m),
    ];

    // V1 placeholder heuristic: a simple three-tier classification of a
    // handful of Brussels-region postal codes, not real market data (price
    // indices, transit access, ...). Meant to be swapped out for a real
    // data source later; kept here so it stays visible and easy to change.
    private static readonly (string PostalCode, string Commune, decimal Points)[] LocationTiers =
    [
        // Tier A
        ("1000", "Bruxelles-Ville", 15m),
        ("1050", "Ixelles", 15m),
        ("1180", "Uccle", 15m),
        ("1200", "Woluwe-Saint-Lambert", 15m),
        // Tier B
        ("1060", "Saint-Gilles", 10m),
        ("1190", "Forest", 10m),
        ("1040", "Etterbeek", 10m),
        ("1170", "Watermael-Boitsfort", 10m),
        // Tier C
        ("1070", "Anderlecht", 6m),
        ("1030", "Schaerbeek", 6m),
        ("1080", "Molenbeek-Saint-Jean", 6m),
        ("1083", "Ganshoren", 6m),
    ];

    private static readonly Dictionary<string, decimal> LocationTierScores =
        LocationTiers.ToDictionary(tier => tier.PostalCode, tier => tier.Points);

    private static readonly string[] UrbanisticKeywords =
    [
        "urbanistique", "urbanisme", "permis d'urbanisme", "infraction urbanistique", "affectation",
        "stedenbouw", "stedenbouwkundig", "ruimtelijke ordening", "omgevingsvergunning", "verkaveling",
    ];

    private static readonly string[] ExplicitUrbanisticRiskPhrases =
    [
        "aucune information urbanistique",
        "sans information urbanistique",
        "extension non documentee",
        "extension non documentée",
        "sans permis",
    ];

    public decimal? EstimateGrossYield(PropertyListing listing)
    {
        var referencePrice = GetReferencePrice(listing);
        if (listing.EstimatedMonthlyRentPerUnit is not (> 0) || referencePrice is not (> 0))
        {
            return null;
        }

        var unitCount = listing.ObservedUnitCount ?? listing.OfficialUnitCount;
        if (unitCount is not (> 0))
        {
            return null;
        }

        var annualRent = listing.EstimatedMonthlyRentPerUnit.Value * 12 * unitCount.Value;
        var acquisitionCost = referencePrice.Value
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
            mediumSeverity.Add($"PEB {listing.PebRating} : rénovation énergétique probablement nécessaire.");
        }

        if (listing.ElectricalInstallationCompliant == false)
        {
            highSeverity.Add("Installation électrique non conforme.");
        }

        var isPublicSale = string.Equals(listing.SaleType, "PublicSale", StringComparison.OrdinalIgnoreCase);

        var hasUnrecognizedUnits = listing.ObservedUnitCount.HasValue && listing.OfficialUnitCount.HasValue &&
                                    listing.ObservedUnitCount.Value > listing.OfficialUnitCount.Value;
        if (hasUnrecognizedUnits)
        {
            highSeverity.Add(
                $"{listing.ObservedUnitCount} logement(s) semblent décrits dans l’annonce, mais seulement " +
                $"{listing.OfficialUnitCount} sont officiellement reconnus — logement(s) potentiellement non reconnus.");
        }

        var mentionsUrbanisticInfo = !string.IsNullOrWhiteSpace(listing.Description) &&
            UrbanisticKeywords.Any(keyword => listing.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        var hasExplicitUrbanisticRisk = !string.IsNullOrWhiteSpace(listing.Description) &&
            ExplicitUrbanisticRiskPhrases.Any(
                phrase => listing.Description.Contains(phrase, StringComparison.OrdinalIgnoreCase));

        if (hasExplicitUrbanisticRisk)
        {
            highSeverity.Add("Information urbanistique manquante ou extension non documentée dans l’annonce.");
        }
        else if (UrbanisticStatusDetector.Detect(listing) == UrbanisticStatus.Infraction)
        {
            highSeverity.Add("Infraction urbanistique ou régularisation mentionnée dans l’annonce.");
        }
        else if (isPublicSale && !mentionsUrbanisticInfo)
        {
            mediumSeverity.Add("Documents urbanistiques non mentionnés dans l’annonce de vente publique.");
        }
        else if (isPublicSale)
        {
            mediumSeverity.Add("Vente publique.");
        }

        if (listing.IsOccupied == true)
        {
            mediumSeverity.Add("Bien occupé.");
        }

        if (listing.AskingPrice is null && listing.CurrentBid is null)
        {
            mediumSeverity.Add("Prix manquant dans l’annonce.");
        }

        if (listing.ObservedUnitCount is > 0 && listing.LivingArea is > 0 &&
            listing.LivingArea.Value / listing.ObservedUnitCount.Value < 15m)
        {
            mediumSeverity.Add("Surface habitable incohérente au regard du nombre de logements annoncé.");
        }

        if (listing.BedroomCount.HasValue && listing.ObservedUnitCount.HasValue &&
            listing.BedroomCount.Value < listing.ObservedUnitCount.Value)
        {
            mediumSeverity.Add(
                $"Données contradictoires : {listing.BedroomCount} chambre(s) annoncée(s) pour " +
                $"{listing.ObservedUnitCount} logement(s) observé(s).");
        }

        var allSignals = highSeverity.Concat(mediumSeverity).ToList();

        var level = highSeverity.Count > 0
            ? RiskLevel.High
            : mediumSeverity.Count > 0
                ? RiskLevel.Medium
                : RiskLevel.Low;

        var summary = allSignals.Count == 0
            ? "Aucune alerte détectée dans les données disponibles. Les documents officiels restent à vérifier."
            : level == RiskLevel.High
                ? $"Alerte majeure à vérifier : {string.Join(" ", allSignals)}"
                : $"Points à vérifier : {string.Join(" ", allSignals)}";

        return new RiskAssessment(level, summary, allSignals);
    }

    public OpportunityScoreBreakdown CalculateOpportunityScore(PropertyListing listing)
    {
        var positiveSignals = new List<string>();
        var missingData = new List<string>();

        var pricePerSquareMeterAvailable = GetReferencePrice(listing) is > 0 && listing.LivingArea is > 0;
        var grossYieldAvailable = EstimateGrossYield(listing).HasValue;
        var unitCountAvailable = (listing.ObservedUnitCount ?? listing.OfficialUnitCount) is > 0;
        var locationAvailable = !string.IsNullOrWhiteSpace(listing.PostalCode) &&
                                LocationTierScores.ContainsKey(listing.PostalCode.Trim());
        var energyAvailable = EnergySteps.Any(step => step.Rating == listing.PebRating);

        var pricePerSqmScore = ScorePricePerSquareMeter(listing, positiveSignals);
        var yieldScore = ScoreGrossYield(listing, positiveSignals);
        var unitCountScore = ScoreUnitCount(listing, positiveSignals);
        var locationScore = ScoreLocation(listing, positiveSignals);
        var energyScore = ScoreEnergy(listing, positiveSignals);

        var riskAssessment = AssessRisk(listing);
        var riskAvailable = HasRiskEvidence(listing, riskAssessment);
        var riskScore = !riskAvailable
            ? 0m
            : RiskSteps.FirstOrDefault(step => step.Level == riskAssessment.RiskLevel).Points;

        if (!pricePerSquareMeterAvailable)
        {
            missingData.Add("Prix de référence et surface habitable nécessaires pour comparer le prix au m².");
        }

        if (!grossYieldAvailable)
        {
            missingData.Add("Loyer mensuel estimé, prix et nombre de logements nécessaires pour le rendement.");
        }

        if (!unitCountAvailable)
        {
            missingData.Add("Nombre de logements non confirmé.");
        }

        if (!locationAvailable)
        {
            missingData.Add("Commune non couverte par l’indice local actuel.");
        }

        if (!energyAvailable)
        {
            missingData.Add("PEB non disponible.");
        }

        if (!riskAvailable)
        {
            missingData.Add("Pas assez d’éléments officiels pour évaluer les points de vigilance.");
        }

        var rawScore = pricePerSqmScore + yieldScore + unitCountScore + locationScore + energyScore + riskScore;
        var availablePoints =
            (pricePerSquareMeterAvailable ? PricePerSquareMeterMaxPoints : 0m) +
            (grossYieldAvailable ? GrossYieldMaxPoints : 0m) +
            (unitCountAvailable ? UnitCountMaxPoints : 0m) +
            (locationAvailable ? LocationMaxPoints : 0m) +
            (energyAvailable ? EnergyMaxPoints : 0m) +
            (riskAvailable ? RiskMaxPoints : 0m);
        var evaluatedCriteriaCount = new[]
        {
            pricePerSquareMeterAvailable,
            grossYieldAvailable,
            unitCountAvailable,
            locationAvailable,
            energyAvailable,
            riskAvailable,
        }.Count(value => value);
        decimal? total = availablePoints >= MinimumAvailablePointsForIndex
            ? Math.Round(rawScore / availablePoints * 100m, 0)
            : null;

        return new OpportunityScoreBreakdown(
            TotalScore: total,
            RawScore: rawScore,
            AvailablePoints: availablePoints,
            DataCompletenessPercentage: availablePoints,
            EvaluatedCriteriaCount: evaluatedCriteriaCount,
            TotalCriteriaCount: 6,
            PricePerSquareMeterScore: pricePerSqmScore,
            PricePerSquareMeterAvailable: pricePerSquareMeterAvailable,
            GrossYieldScore: yieldScore,
            GrossYieldAvailable: grossYieldAvailable,
            UnitCountScore: unitCountScore,
            UnitCountAvailable: unitCountAvailable,
            LocationScore: locationScore,
            LocationAvailable: locationAvailable,
            EnergyScore: energyScore,
            EnergyAvailable: energyAvailable,
            RiskScore: riskScore,
            RiskAvailable: riskAvailable,
            MissingData: missingData,
            PositiveSignals: positiveSignals,
            RiskSignals: riskAssessment.Signals);
    }

    public IndexMethodologyDto DescribeIndex()
    {
        static string Eur(decimal value) => value.ToString("N0", FrenchBelgium);

        static IndexStepDto[] WithFallback(IEnumerable<IndexStepDto> steps, string fallback) =>
            [.. steps, new IndexStepDto(fallback, 0m)];

        return new IndexMethodologyDto(
            TotalPoints: PricePerSquareMeterMaxPoints + GrossYieldMaxPoints + UnitCountMaxPoints +
                         LocationMaxPoints + EnergyMaxPoints + RiskMaxPoints,
            MinimumAvailablePoints: MinimumAvailablePointsForIndex,
            StrongOpportunityThreshold: StrongOpportunityThreshold,
            Criteria:
            [
                new IndexCriterionDto(
                    "pricePerSquareMeter", "Prix au m²", PricePerSquareMeterMaxPoints,
                    "Prix de référence divisé par la surface habitable. En vente publique, le prix de référence " +
                    "est l’estimation finale si elle existe, sinon l’enchère en cours.",
                    "Le prix et la surface habitable sont connus.",
                    WithFallback(
                        PricePerSquareMeterSteps.Select(step => new IndexStepDto($"jusqu’à {Eur(step.Max)} €/m²", step.Points)),
                        $"au-delà de {Eur(PricePerSquareMeterSteps[^1].Max)} €/m²")),
                new IndexCriterionDto(
                    "grossYield", "Rendement brut estimé", GrossYieldMaxPoints,
                    "Loyer annuel (loyer mensuel par logement × 12 × nombre de logements) divisé par le prix, " +
                    "les frais d’acquisition et le budget de rénovation que vous avez saisis.",
                    "Vous avez saisi un loyer mensuel sur la fiche, et le prix et le nombre de logements sont connus.",
                    WithFallback(
                        GrossYieldSteps.Select(step => new IndexStepDto($"{step.Min:0} % et plus", step.Points)),
                        $"moins de {GrossYieldSteps[^1].Min:0} %")),
                new IndexCriterionDto(
                    "unitCount", "Nombre de logements", UnitCountMaxPoints,
                    "Nombre de logements décrits dans l’annonce, à défaut celui des documents officiels.",
                    "Un nombre de logements est connu.",
                    WithFallback(
                        UnitCountSteps.Select(step => new IndexStepDto($"{step.Min} logements et plus", step.Points)),
                        "1 logement")),
                new IndexCriterionDto(
                    "location", "Localisation", LocationMaxPoints,
                    "Classement fixe de quelques communes bruxelloises en trois niveaux. Ce n’est pas une donnée de marché.",
                    "Le code postal fait partie de la liste ci-dessous. Partout ailleurs, le critère est ignoré.",
                    [.. LocationTiers.Select(tier => new IndexStepDto($"{tier.PostalCode} {tier.Commune}", tier.Points))]),
                new IndexCriterionDto(
                    "energy", "Performance énergétique", EnergyMaxPoints,
                    "Classe PEB indiquée par la source.",
                    "Le PEB est connu.",
                    [.. EnergySteps.Select(step => new IndexStepDto($"PEB {step.Rating}", step.Points))]),
                new IndexCriterionDto(
                    "risk", "Points de vigilance", RiskMaxPoints,
                    "Niveau de vigilance déduit de l’annonce : infraction urbanistique, électricité non conforme, " +
                    "logements non reconnus, vente publique, bien occupé, prix manquant, données incohérentes.",
                    "Au moins un signal a été relevé, ou le PEB, la conformité électrique, l’occupation ou les deux " +
                    "nombres de logements (annoncé et officiel) sont connus.",
                    [.. RiskSteps.Select(step => new IndexStepDto(step.Label, step.Points))]),
            ]);
    }

    public void Analyze(PropertyListing listing)
    {
        listing.EstimatedGrossYield = EstimateGrossYield(listing);

        listing.UrbanisticStatus = UrbanisticStatusDetector.Detect(listing);

        var risk = AssessRisk(listing);
        listing.RiskLevel = risk.RiskLevel;
        listing.RiskSummary = risk.RiskSummary;

        listing.OpportunityScore = CalculateOpportunityScore(listing).TotalScore;
    }

    private static decimal ScorePricePerSquareMeter(PropertyListing listing, List<string> positiveSignals)
    {
        var referencePrice = GetReferencePrice(listing);
        if (referencePrice is not (> 0) || listing.LivingArea is not (> 0))
        {
            return 0m;
        }

        var pricePerSqm = referencePrice.Value / listing.LivingArea.Value;

        var score = PricePerSquareMeterSteps.FirstOrDefault(step => pricePerSqm <= step.Max).Points;

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

        var score = GrossYieldSteps.FirstOrDefault(step => grossYield.Value >= step.Min).Points;

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

        var score = UnitCountSteps.FirstOrDefault(step => unitCount.Value >= step.Min).Points;

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
            return 0m;
        }

        if (score >= 15m)
        {
            positiveSignals.Add($"Localisation ({listing.City}) consideree comme premium.");
        }

        return score;
    }

    private static decimal ScoreEnergy(PropertyListing listing, List<string> positiveSignals)
    {
        var score = EnergySteps.FirstOrDefault(step => step.Rating == listing.PebRating).Points;

        if (score >= 8m)
        {
            positiveSignals.Add($"Bonne performance energetique (PEB {listing.PebRating}).");
        }

        return score;
    }

    private static bool HasRiskEvidence(PropertyListing listing, RiskAssessment assessment) =>
        assessment.Signals.Count > 0 ||
        listing.PebRating is not null ||
        listing.ElectricalInstallationCompliant.HasValue ||
        listing.IsOccupied.HasValue ||
        (listing.ObservedUnitCount.HasValue && listing.OfficialUnitCount.HasValue);

    private static decimal? GetReferencePrice(PropertyListing listing)
    {
        if (string.Equals(listing.SaleType, "PublicSale", StringComparison.OrdinalIgnoreCase))
        {
            if (listing.EstimatedFinalPrice is > 0)
            {
                return listing.EstimatedFinalPrice;
            }

            if (listing.CurrentBid is > 0)
            {
                return listing.CurrentBid;
            }
        }

        return listing.AskingPrice is > 0 ? listing.AskingPrice : null;
    }

}
