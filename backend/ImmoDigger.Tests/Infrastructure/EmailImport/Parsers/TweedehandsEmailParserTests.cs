using ImmoDigger.Application.DTOs;
using ImmoDigger.Infrastructure.EmailImport.Parsers;

namespace ImmoDigger.Tests.Infrastructure.EmailImport.Parsers;

public class TweedehandsEmailParserTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "EmailImport", fileName));

    [Theory]
    [InlineData("alerts@2ememain.be", true)]
    [InlineData("no-reply@2ememain.be", true)]
    [InlineData("alerts@2dehands.be", false)] // Dutch mirror domain not covered yet - see class doc
    [InlineData("alerts@immoweb.be", false)]
    public void CanParse_OnlyRecognizesThe2ememainDomain(string sender, bool expected)
    {
        var parser = new TweedehandsEmailParser();

        Assert.Equal(expected, parser.CanParse(sender));
    }

    [Fact]
    public void Parse_ExtractsTheSingleListing_FromAFictionalAlertEmail()
    {
        var parser = new TweedehandsEmailParser();
        var message = new EmailMessage
        {
            MessageId = "<msg-1@2ememain.be>",
            Sender = "alerts@2ememain.be",
            Subject = "Nouvelles annonces pour votre recherche",
            HtmlBody = ReadFixture("tweedehands-alert.html"),
            ReceivedAt = new DateTime(2026, 8, 7, 9, 0, 0, DateTimeKind.Utc),
        };

        var results = parser.Parse(message);

        var listing = Assert.Single(results);
        Assert.Equal("2ememain", listing.Source);
        Assert.Equal("1987654", listing.ExternalId);
        Assert.Equal(
            "https://www.2ememain.be/v/immo/maisons-a-vendre/m1987654-maison-fictive-test", listing.Url);
        Assert.Equal("MAISON FICTIVE DE TEST - PARTICULIER", listing.Title);
        Assert.Equal("https://example-test.invalid/2ememain/1987654-cover.jpg", listing.ImageUrl);
        Assert.Equal("1090", listing.PostalCode);
        Assert.Equal("Jette", listing.City);
        Assert.Equal(275_000m, listing.AskingPrice);
        Assert.Equal("House", listing.PropertyType);
        Assert.Equal("<msg-1@2ememain.be>", listing.EmailMessageId);
    }

    [Fact]
    public void Parse_ReturnsEmpty_WhenTheMessageHasNoHtmlBody()
    {
        var parser = new TweedehandsEmailParser();
        var message = new EmailMessage { MessageId = "<msg-2@2ememain.be>", Sender = "alerts@2ememain.be" };

        Assert.Empty(parser.Parse(message));
    }
}
