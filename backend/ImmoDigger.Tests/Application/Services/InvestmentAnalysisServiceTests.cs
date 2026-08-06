using ImmoDigger.Application.Services;
using ImmoDigger.Domain.Common;
using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Tests.Application.Services;

public class InvestmentAnalysisServiceTests
{
    private readonly InvestmentAnalysisService _sut = new();

    private static PropertyListing CreateListing() => new()
    {
        Source = "Immoweb",
        ExternalId = "EXT-1",
        Url = "https://example.invalid/listing/1",
        Title = "Immeuble de rapport",
        SaleType = "RegularSale",
        PropertyType = "IncomeBuilding",
        RawContentHash = "hash-1",
        FirstSeenAt = DateTime.UtcNow,
        LastSeenAt = DateTime.UtcNow,
    };

    // --- Gross yield ------------------------------------------------------

    [Fact]
    public void EstimateGrossYield_ReturnsNull_WhenRentIsMissing()
    {
        var listing = CreateListing();
        listing.AskingPrice = 400_000m;
        listing.ObservedUnitCount = 4;

        Assert.Null(_sut.EstimateGrossYield(listing));
    }

    [Fact]
    public void EstimateGrossYield_ReturnsNull_WhenPriceIsMissing()
    {
        var listing = CreateListing();
        listing.EstimatedMonthlyRentPerUnit = 750m;
        listing.ObservedUnitCount = 4;

        Assert.Null(_sut.EstimateGrossYield(listing));
    }

    [Fact]
    public void EstimateGrossYield_ReturnsNull_WhenUnitCountIsMissing()
    {
        var listing = CreateListing();
        listing.AskingPrice = 400_000m;
        listing.EstimatedMonthlyRentPerUnit = 750m;

        Assert.Null(_sut.EstimateGrossYield(listing));
    }

    [Fact]
    public void EstimateGrossYield_ComputesExpectedPercentage()
    {
        var listing = CreateListing();
        listing.AskingPrice = 400_000m;
        listing.EstimatedMonthlyRentPerUnit = 750m;
        listing.ObservedUnitCount = 4;

        // annual rent = 750 * 12 * 4 = 36 000
        // acquisition cost = 400 000
        // yield = 36 000 / 400 000 = 9 %
        Assert.Equal(9m, _sut.EstimateGrossYield(listing));
    }

    [Fact]
    public void EstimateGrossYield_IncludesAcquisitionCostsAndRenovationBudget()
    {
        var listing = CreateListing();
        listing.AskingPrice = 400_000m;
        listing.EstimatedAcquisitionCosts = 40_000m;
        listing.EstimatedRenovationBudget = 60_000m;
        listing.EstimatedMonthlyRentPerUnit = 750m;
        listing.ObservedUnitCount = 4;

        // acquisition cost = 400 000 + 40 000 + 60 000 = 500 000
        // yield = 36 000 / 500 000 = 7.2 %
        Assert.Equal(7.2m, _sut.EstimateGrossYield(listing));
    }

    [Fact]
    public void EstimateGrossYield_FallsBackToOfficialUnitCount_WhenObservedIsMissing()
    {
        var listing = CreateListing();
        listing.AskingPrice = 300_000m;
        listing.EstimatedMonthlyRentPerUnit = 625m;
        listing.OfficialUnitCount = 3;

        // annual rent = 625 * 12 * 3 = 22 500 ; yield = 22 500 / 300 000 = 7.5 %
        Assert.Equal(7.5m, _sut.EstimateGrossYield(listing));
    }

    // --- Risk assessment ----------------------------------------------------

    [Fact]
    public void AssessRisk_ReturnsLow_WhenNoSignalIsDetected()
    {
        var listing = CreateListing();
        listing.PebRating = "B";
        listing.ElectricalInstallationCompliant = true;
        listing.AskingPrice = 400_000m;

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.Low, result.RiskLevel);
        Assert.Empty(result.Signals);
    }

    [Theory]
    [InlineData("F")]
    [InlineData("G")]
    public void AssessRisk_ReturnsHigh_ForPoorPebRating(string pebRating)
    {
        var listing = CreateListing();
        listing.PebRating = pebRating;

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains(result.Signals, s => s.Contains("energetique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssessRisk_ReturnsHigh_ForNonCompliantElectricalInstallation()
    {
        var listing = CreateListing();
        listing.ElectricalInstallationCompliant = false;

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains(result.Signals, s => s.Contains("electrique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssessRisk_ReturnsHigh_WhenObservedUnitsExceedOfficialUnits()
    {
        var listing = CreateListing();
        listing.ObservedUnitCount = 4;
        listing.OfficialUnitCount = 3;

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains(result.Signals, s => s.Contains("non reconnu", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssessRisk_ReturnsHigh_ForPublicSaleWithoutUrbanisticInfo()
    {
        var listing = CreateListing();
        listing.SaleType = "PublicSale";
        listing.Description = "Belle maison de rapport a renover.";

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains(result.Signals, s => s.Contains("urbanistique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssessRisk_ReturnsMedium_ForPublicSale_WhenUrbanisticInfoIsMentioned()
    {
        var listing = CreateListing();
        listing.SaleType = "PublicSale";
        listing.Description = "Aucune infraction urbanistique connue a ce jour.";

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
    }

    [Fact]
    public void AssessRisk_ReturnsMedium_ForOccupiedProperty()
    {
        var listing = CreateListing();
        listing.IsOccupied = true;

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
        Assert.Contains(result.Signals, s => s.Contains("occupe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssessRisk_ReturnsMedium_WhenPriceIsMissing()
    {
        var listing = CreateListing();
        listing.AskingPrice = null;
        listing.CurrentBid = null;

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
        Assert.Contains(result.Signals, s => s.Contains("Prix manquant", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssessRisk_ReturnsMedium_ForContradictoryBedroomAndUnitCounts()
    {
        var listing = CreateListing();
        listing.BedroomCount = 2;
        listing.ObservedUnitCount = 4;

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
        Assert.Contains(result.Signals, s => s.Contains("contradictoires", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AssessRisk_SummaryReflectsTheHighestSeveritySignal()
    {
        var listing = CreateListing();
        listing.PebRating = "G"; // high
        listing.IsOccupied = true; // medium

        var result = _sut.AssessRisk(listing);

        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.StartsWith("Risque eleve", result.RiskSummary);
    }

    // --- Opportunity score ----------------------------------------------------

    [Fact]
    public void CalculateOpportunityScore_NeverExceeds100()
    {
        var listing = CreateListing();
        listing.AskingPrice = 100_000m;
        listing.LivingArea = 200m; // 500 EUR/m2 -> full price score
        listing.EstimatedMonthlyRentPerUnit = 2000m;
        listing.ObservedUnitCount = 8; // full unit-count score, excellent yield
        listing.PostalCode = "1000"; // full location score
        listing.PebRating = "A"; // full energy score
        listing.ElectricalInstallationCompliant = true;

        var breakdown = _sut.CalculateOpportunityScore(listing);

        Assert.True(breakdown.TotalScore <= 100m);
        Assert.Equal(
            breakdown.PricePerSquareMeterScore + breakdown.GrossYieldScore + breakdown.UnitCountScore +
            breakdown.LocationScore + breakdown.EnergyScore + breakdown.RiskScore,
            breakdown.TotalScore);
    }

    [Fact]
    public void CalculateOpportunityScore_AwardsZeroPricePerSquareMeterPoints_WhenDataIsMissing()
    {
        var listing = CreateListing();

        var breakdown = _sut.CalculateOpportunityScore(listing);

        Assert.Equal(0m, breakdown.PricePerSquareMeterScore);
    }

    [Fact]
    public void CalculateOpportunityScore_UsesKnownLocationTier()
    {
        var listing = CreateListing();
        listing.PostalCode = "1180"; // Uccle: tier A
        listing.City = "Uccle";

        var breakdown = _sut.CalculateOpportunityScore(listing);

        Assert.Equal(15m, breakdown.LocationScore);
    }

    [Fact]
    public void CalculateOpportunityScore_UsesNeutralDefault_ForUnknownPostalCode()
    {
        var listing = CreateListing();
        listing.PostalCode = "9999";

        var breakdown = _sut.CalculateOpportunityScore(listing);

        Assert.Equal(5m, breakdown.LocationScore);
    }

    [Fact]
    public void CalculateOpportunityScore_EnergyScore_IsHighestForPebA_AndZeroForPebG()
    {
        var listingA = CreateListing();
        listingA.PebRating = "A";
        var listingG = CreateListing();
        listingG.PebRating = "G";

        Assert.Equal(10m, _sut.CalculateOpportunityScore(listingA).EnergyScore);
        Assert.Equal(0m, _sut.CalculateOpportunityScore(listingG).EnergyScore);
    }

    [Fact]
    public void CalculateOpportunityScore_RiskScore_IsZero_WhenRiskIsHigh()
    {
        var listing = CreateListing();
        listing.ElectricalInstallationCompliant = false;

        var breakdown = _sut.CalculateOpportunityScore(listing);

        Assert.Equal(0m, breakdown.RiskScore);
        Assert.NotEmpty(breakdown.RiskSignals);
    }

    [Fact]
    public void CalculateOpportunityScore_RiskScore_IsFull_WhenRiskIsLow()
    {
        var listing = CreateListing();
        listing.ElectricalInstallationCompliant = true;
        listing.AskingPrice = 400_000m;

        var breakdown = _sut.CalculateOpportunityScore(listing);

        Assert.Equal(10m, breakdown.RiskScore);
    }

    [Fact]
    public void CalculateOpportunityScore_IncludesPositiveSignals_WhenComponentsScoreWell()
    {
        var listing = CreateListing();
        listing.AskingPrice = 300_000m;
        listing.LivingArea = 250m; // 1200 EUR/m2 -> strong score
        listing.PebRating = "A";

        var breakdown = _sut.CalculateOpportunityScore(listing);

        Assert.NotEmpty(breakdown.PositiveSignals);
    }

    // --- Analyze (orchestration) --------------------------------------------

    [Fact]
    public void Analyze_WritesComputedResultsOntoTheListing()
    {
        var listing = CreateListing();
        listing.AskingPrice = 400_000m;
        listing.EstimatedMonthlyRentPerUnit = 750m;
        listing.ObservedUnitCount = 4;
        listing.PebRating = "G";

        _sut.Analyze(listing);

        Assert.Equal(9m, listing.EstimatedGrossYield);
        Assert.Equal(RiskLevel.High, listing.RiskLevel);
        Assert.False(string.IsNullOrWhiteSpace(listing.RiskSummary));
        Assert.NotNull(listing.OpportunityScore);
    }
}
