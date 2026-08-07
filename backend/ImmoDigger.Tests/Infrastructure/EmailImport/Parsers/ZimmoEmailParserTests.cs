using ImmoDigger.Application.DTOs;
using ImmoDigger.Infrastructure.EmailImport.Parsers;

namespace ImmoDigger.Tests.Infrastructure.EmailImport.Parsers;

public class ZimmoEmailParserTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "EmailImport", fileName));

    [Fact]
    public void Parse_ExtractsTheSingleListing_FromAFictionalAlertEmail()
    {
        var parser = new ZimmoEmailParser();
        var message = new EmailMessage
        {
            MessageId = "<msg-1@zimmo.be>",
            Sender = "alerts@zimmo.be",
            Subject = "Nieuwe panden",
            HtmlBody = ReadFixture("zimmo-alert.html"),
            ReceivedAt = new DateTime(2026, 8, 6, 9, 0, 0, DateTimeKind.Utc),
        };

        var results = parser.Parse(message);

        var listing = Assert.Single(results);
        Assert.Equal("Zimmo", listing.Source);
        Assert.Equal("8845221", listing.ExternalId);
        Assert.Equal("https://www.zimmo.be/en/brussels-1000/for-sale/house/8845221/", listing.Url);
        Assert.Equal("MAISON FICTIVE DE TEST - ZIMMO", listing.Title);
        Assert.Equal("1000", listing.PostalCode);
        Assert.Equal("Bruxelles", listing.City);
        Assert.Equal(380_000m, listing.AskingPrice);
        Assert.Equal("House", listing.PropertyType);
    }
}
