using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Infrastructure.EmailImport.Parsers;

namespace ImmoDigger.Tests.Infrastructure.EmailImport.Parsers;

public class AdditionalPortalEmailParserTests
{
    [Fact]
    public void Spotto_ExtractsCurrentAlphanumericDetailId_AndInvestmentFields()
    {
        var listing = ParseSingle(
            new SpottoEmailParser(),
            "alerts@notify.spotto.be",
            """
            <div>
              <h2>Immeuble de rapport à Bruxelles</h2>
              <p>795 000 € · 1000 Bruxelles · 10 chambres · 375 m² · PEB D</p>
              <a href="https://www.spotto.be/nl/p/te-koop/1000-bruxelles/huis-rue-du-marteau-16/PGeFywlxTEKbfwjdgE-tKg">Bekijk pand</a>
            </div>
            """);

        Assert.Equal("Spotto", listing.Source);
        Assert.Equal("PGeFywlxTEKbfwjdgE-tKg", listing.ExternalId);
        Assert.Equal(795_000m, listing.AskingPrice);
        Assert.Equal(10, listing.BedroomCount);
        Assert.Equal(375m, listing.LivingArea);
        Assert.Equal("D", listing.PebRating);
        Assert.Equal("IncomeBuilding", listing.PropertyType);
    }

    [Fact]
    public void Immoscoop_ExtractsFrenchDetailUrl()
    {
        var listing = ParseSingle(
            new ImmoscoopEmailParser(),
            "notifications@immoscoop.be",
            """
            <table><tr><td>
              <h3>Maison divisée en studios</h3>
              <p>580.000 EUR — 1000 Bruxelles — Surface habitable: 195 m² — 4 chambres — PEB G</p>
              <a href="https://www.immoscoop.be/fr/a-vendre/1000-bruxelles/598824">Voir le bien</a>
            </td></tr></table>
            """);

        Assert.Equal("Immoscoop", listing.Source);
        Assert.Equal("598824", listing.ExternalId);
        Assert.Equal(580_000m, listing.AskingPrice);
        Assert.Equal("1000", listing.PostalCode);
        Assert.Equal("G", listing.PebRating);
    }

    [Fact]
    public void Realo_DecodesAUrlWrappedByEmailClickTracking()
    {
        var destination = Uri.EscapeDataString(
            "https://www.realo.be/fr/avenue-de-loree-7-1000-bruxelles/69551?l=10120787");
        var listing = ParseSingle(
            new RealoEmailParser(),
            "alerts@mailer.realo.be",
            $"""
             <div>
               <h2>Élégante maison Art Déco</h2>
               <p>1 850 000 € · 1000 Bruxelles · 420 m² · 4 chambres</p>
               <a href="https://click.realo.be/redirect?destination={destination}">Consulter</a>
             </div>
             """);

        Assert.Equal("Realo", listing.Source);
        Assert.Equal("69551", listing.ExternalId);
        Assert.Equal(1_850_000m, listing.AskingPrice);
        Assert.StartsWith("https://www.realo.be/", listing.Url);
    }

    [Fact]
    public void Immoweb_ParsesTextOnlyAlerts()
    {
        var parser = new ImmowebEmailParser();
        var message = new EmailMessage
        {
            MessageId = "<text@immoweb.be>",
            Sender = "alerts@immoweb.be",
            Subject = "Nouveau bien",
            TextBody = """
                       Immeuble de rapport à Ixelles
                       1 495 000 € - 1050 Ixelles - 570 m² - 5 chambres - PEB E
                       https://www.immoweb.be/fr/annonce/immeuble-a-appartements/a-vendre/ixelles/1050/21638355
                       """,
            ReceivedAt = DateTime.UtcNow,
        };

        var listing = Assert.Single(parser.Parse(message));
        Assert.Equal("21638355", listing.ExternalId);
        Assert.Equal(1_495_000m, listing.AskingPrice);
        Assert.Equal("E", listing.PebRating);
    }

    [Theory]
    [InlineData("alerts@immoweb.be", true)]
    [InlineData("alerts@mail.immoweb.be", true)]
    [InlineData("alerts@immoweb.be.attacker.example", false)]
    public void SenderMatching_RequiresTheRealDomainBoundary(string sender, bool expected)
    {
        Assert.Equal(expected, new ImmowebEmailParser().CanParse(sender));
    }

    [Fact]
    public void Zimmo_AcceptsCurrentShortAlphanumericIds()
    {
        var listing = ParseSingle(
            new ZimmoEmailParser(),
            "alerts@zimmo.be",
            """
            <div><h2>Maison à Anderlecht</h2><p>499.000 € 1070 Anderlecht</p>
            <a href="https://www.zimmo.be/fr/anderlecht-1070/a-vendre/maison/KU4PS/">Voir</a></div>
            """);

        Assert.Equal("KU4PS", listing.ExternalId);
    }

    [Theory]
    [InlineData("alerts@2ememain.be", true)]
    [InlineData("alerts@2dehands.be", true)]
    public void Tweedehands_AcceptsBothLanguageDomains(string sender, bool expected)
    {
        Assert.Equal(expected, new TweedehandsEmailParser().CanParse(sender));
    }

    private static CollectedListing ParseSingle(IEmailListingParser parser, string sender, string html)
    {
        var message = new EmailMessage
        {
            MessageId = $"<test@{parser.SourceName.ToLowerInvariant()}>",
            Sender = sender,
            Subject = "Nouveau bien",
            HtmlBody = html,
            ReceivedAt = DateTime.UtcNow,
        };

        return Assert.Single(parser.Parse(message));
    }
}
