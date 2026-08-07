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
}
