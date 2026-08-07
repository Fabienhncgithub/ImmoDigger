using System.Text.RegularExpressions;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Infrastructure.EmailImport.Parsers;

namespace ImmoDigger.Tests.Infrastructure.EmailImport.Parsers;

public class AgencyEmailParserTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "EmailImport", fileName));

    private static AgencyEmailParser CreateDupontImmoParser() =>
        new("AgenceDupont", "dupont-immo.be", new Regex(@"dupont-immo\.be/biens/(?<id>\d+)", RegexOptions.IgnoreCase));

    [Fact]
    public void Parse_ExtractsTheSingleListing_UsingTheAgencysOwnUrlPattern()
    {
        var parser = CreateDupontImmoParser();
        var message = new EmailMessage
        {
            MessageId = "<msg-1@dupont-immo.be>",
            Sender = "newsletter@dupont-immo.be",
            Subject = "Nos nouveautes",
            HtmlBody = ReadFixture("agency-alert.html"),
            ReceivedAt = new DateTime(2026, 8, 6, 9, 0, 0, DateTimeKind.Utc),
        };

        var results = parser.Parse(message);

        var listing = Assert.Single(results);
        Assert.Equal("AgenceDupont", listing.Source);
        Assert.Equal("4242", listing.ExternalId);
        Assert.Equal("https://www.dupont-immo.be/biens/4242", listing.Url);
        Assert.Equal("STUDIO FICTIF DE TEST - AGENCE DUPONT", listing.Title);
        Assert.Equal(165_000m, listing.AskingPrice);
    }

    [Fact]
    public void CanParse_OnlyRecognizesTheConfiguredDomain()
    {
        var parser = CreateDupontImmoParser();

        Assert.True(parser.CanParse("newsletter@dupont-immo.be"));
        Assert.False(parser.CanParse("alerts@immoweb.be"));
    }
}
