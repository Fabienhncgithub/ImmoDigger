using ImmoDigger.Application.DTOs;
using ImmoDigger.Infrastructure.EmailImport.Parsers;

namespace ImmoDigger.Tests.Infrastructure.EmailImport.Parsers;

public class ImmovlanEmailParserTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "EmailImport", fileName));

    [Fact]
    public void Parse_ExtractsTheSingleListing_FromAFictionalAlertEmail()
    {
        var parser = new ImmovlanEmailParser();
        var message = new EmailMessage
        {
            MessageId = "<msg-1@immovlan.be>",
            Sender = "alerts@immovlan.be",
            Subject = "Nieuwe zoekertjes",
            HtmlBody = ReadFixture("immovlan-alert.html"),
            ReceivedAt = new DateTime(2026, 8, 6, 9, 0, 0, DateTimeKind.Utc),
        };

        var results = parser.Parse(message);

        var listing = Assert.Single(results);
        Assert.Equal("Immovlan", listing.Source);
        Assert.Equal("rbf98765", listing.ExternalId);
        Assert.Equal(
            "https://www.immovlan.be/en/detail/apartment/for-sale/1080/molenbeek/rbf98765", listing.Url);
        Assert.Equal("APPARTEMENT FICTIF DE TEST - MOLENBEEK", listing.Title);
        Assert.Equal("1080", listing.PostalCode);
        Assert.Equal("Molenbeek", listing.City);
        Assert.Equal(245_000m, listing.AskingPrice);
        Assert.Equal("Apartment", listing.PropertyType);
    }

    [Fact]
    public void CanParse_RejectsAnUnrelatedSender()
    {
        Assert.False(new ImmovlanEmailParser().CanParse("someone@example.com"));
    }

    [Fact]
    public void Parse_ExtractsCurrentTrackedCard_WithoutFollowingTheTrackingLink()
    {
        var parser = new ImmovlanEmailParser();
        var message = new EmailMessage
        {
            MessageId = "<tracked-alert@immovlan.be>",
            Sender = "newsletter@news.immovlan.be",
            Subject = "4 nouveaux biens sur Immovlan.be",
            HtmlBody = ReadFixture("immovlan-tracked-alert.html"),
            ReceivedAt = new DateTime(2026, 8, 20, 5, 0, 0, DateTimeKind.Utc),
        };

        var listing = Assert.Single(parser.Parse(message));

        Assert.Equal("RBT12345", listing.ExternalId);
        Assert.Equal("Immeuble de rapport à vendre à Laeken, 1020", listing.Title);
        Assert.Equal("1020", listing.PostalCode);
        Assert.Equal("Laeken", listing.City);
        Assert.Equal(900_000m, listing.AskingPrice);
        Assert.Equal(12, listing.BedroomCount);
        Assert.Equal(920m, listing.LivingArea);
        Assert.Equal("IncomeBuilding", listing.PropertyType);
        Assert.Equal(
            "https://api-image.immovlan.be/v1/property/RBT12345/thumbnail/medium?h=280",
            listing.ImageUrl);
        Assert.StartsWith("https://r.btk3.immovlan.be/tr/cl/", listing.Url);
    }

    [Fact]
    public void Parse_RejectsLookalikeTrackingHost()
    {
        var html = ReadFixture("immovlan-tracked-alert.html")
            .Replace("r.btk3.immovlan.be", "r.btk3.immovlan.be.example.com", StringComparison.Ordinal);
        var parser = new ImmovlanEmailParser();
        var message = new EmailMessage
        {
            MessageId = "<lookalike@immovlan.be>",
            Sender = "newsletter@immovlan.be",
            Subject = "Alerte",
            HtmlBody = html,
            ReceivedAt = DateTime.UtcNow,
        };

        Assert.Empty(parser.Parse(message));
    }

    [Fact]
    public void Parse_DoesNotConfusePostalCodeWithMultiMillionPrice()
    {
        var html = ReadFixture("immovlan-tracked-alert.html")
            .Replace("Laeken, 1020", "Bruxelles, 1000", StringComparison.Ordinal)
            .Replace("12 chambres · 920 m²", string.Empty, StringComparison.Ordinal)
            .Replace("€ 900.000", "€ 2.400.000", StringComparison.Ordinal);
        var parser = new ImmovlanEmailParser();
        var message = new EmailMessage
        {
            MessageId = "<multi-million@immovlan.be>",
            Sender = "newsletter@immovlan.be",
            Subject = "Alerte",
            HtmlBody = html,
            ReceivedAt = DateTime.UtcNow,
        };

        var listing = Assert.Single(parser.Parse(message));

        Assert.Equal("Immeuble de rapport à vendre à Bruxelles, 1000", listing.Title);
        Assert.Equal("1000", listing.PostalCode);
        Assert.Equal(2_400_000m, listing.AskingPrice);
        Assert.Equal(
            "https://api-image.immovlan.be/v1/property/RBT12345/thumbnail/medium?h=280",
            listing.ImageUrl);
    }
}
