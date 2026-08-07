using ImmoDigger.Application.DTOs;
using ImmoDigger.Infrastructure.EmailImport.Parsers;

namespace ImmoDigger.Tests.Infrastructure.EmailImport.Parsers;

public class ImmowebEmailParserTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "EmailImport", fileName));

    [Theory]
    [InlineData("alerts@immoweb.be", true)]
    [InlineData("no-reply@immoweb.be", true)]
    [InlineData("alerts@immovlan.be", false)]
    public void CanParse_OnlyRecognizesTheImmowebDomain(string sender, bool expected)
    {
        var parser = new ImmowebEmailParser();

        Assert.Equal(expected, parser.CanParse(sender));
    }

    [Fact]
    public void Parse_ExtractsTheSingleListing_FromAFictionalAlertEmail()
    {
        var parser = new ImmowebEmailParser();
        var message = new EmailMessage
        {
            MessageId = "<msg-1@immoweb.be>",
            Sender = "alerts@immoweb.be",
            Subject = "Nouveaux biens correspondant a votre recherche",
            HtmlBody = ReadFixture("immoweb-alert.html"),
            ReceivedAt = new DateTime(2026, 8, 6, 9, 0, 0, DateTimeKind.Utc),
        };

        var results = parser.Parse(message);

        var listing = Assert.Single(results);
        Assert.Equal("Immoweb", listing.Source);
        Assert.Equal("8845221", listing.ExternalId);
        Assert.Equal("https://www.immoweb.be/en/classified/house/for-sale/brussels/1050/8845221", listing.Url);
        Assert.Equal("MAISON FICTIVE DE TEST - CHARME PRES DE FLAGEY", listing.Title);
        Assert.Equal("https://example-test.invalid/immoweb/8845221-cover.jpg", listing.ImageUrl);
        Assert.Equal("1050", listing.PostalCode);
        Assert.Equal("Ixelles", listing.City);
        Assert.Equal(495_000m, listing.AskingPrice);
        Assert.Equal("House", listing.PropertyType);
        Assert.Equal("<msg-1@immoweb.be>", listing.EmailMessageId);
        Assert.Equal("alerts@immoweb.be", listing.EmailSender);
        Assert.False(string.IsNullOrWhiteSpace(listing.RawContentHash));
    }

    [Fact]
    public void Parse_ReturnsEmpty_WhenTheMessageHasNoHtmlBody()
    {
        var parser = new ImmowebEmailParser();
        var message = new EmailMessage { MessageId = "<msg-2@immoweb.be>", Sender = "alerts@immoweb.be" };

        Assert.Empty(parser.Parse(message));
    }
}
