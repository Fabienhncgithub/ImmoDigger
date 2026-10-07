using ImmoDigger.Application.Services;
using ImmoDigger.Domain.Common;

namespace ImmoDigger.Tests.Application.Services;

public class UrbanisticStatusDetectorTests
{
    [Theory]
    // Real wording seen in collected listings.
    [InlineData("RU 2023 : maison unifamiliale  Infraction urbanistique : division de l’immeuble en 4 appartements régularisation à charge de l’acquéreur")]
    [InlineData("IMMEUBLE DE RAPPORT – 3 UNITÉS PERMIS EN COURS 599 000 €")]
    [InlineData("Immeuble de 4 appartements, situation à régulariser.")]
    [InlineData("A REGULARISER : annexe arrière")]
    [InlineData("Un dossier de régularisation a été introduit.")]
    [InlineData("3 logements dont 1 non reconnu à l'urbanisme")]
    [InlineData("Combles aménagés sans permis.")]
    [InlineData("Non-conformité urbanistique constatée.")]
    [InlineData("Bien non conforme à l'urbanisme")]
    [InlineData("Er werd een bouwovertreding vastgesteld.")]
    [InlineData("Achterbouw te regulariseren door de koper.")]
    [InlineData("Dagvaarding werd uitgebracht.")]
    [InlineData("Bijgebouw niet vergund.")]
    public void Detect_ReturnsInfraction_WhenAnInfractionOrRegularisationIsMentioned(string text) =>
        Assert.Equal(UrbanisticStatus.Infraction, UrbanisticStatusDetector.Detect(text));

    [Theory]
    [InlineData("Pas d'infraction urbanistique.")]
    [InlineData("Aucune infraction urbanistique n'a été constatée.")]
    [InlineData("Bien sans infraction, 3 unités.")]
    [InlineData("Infractions urbanistiques : néant")]
    [InlineData("Immeuble de rapport, tout est en ordre.")]
    [InlineData("Urbanisme : en ordre")]
    [InlineData("En ordre au niveau urbanistique")]
    [InlineData("Conforme à l'urbanisme, 4 logements reconnus.")]
    [InlineData("Situation urbanistique régulière")]
    [InlineData("3 unités reconnues à l'urbanisme")]
    [InlineData("Immeuble de rapport avec 2 unités reconnues avec jardin")]
    [InlineData("Situation régularisée en 2021.")]
    [InlineData("Geen stedenbouwkundige overtreding vastgesteld.")]
    [InlineData("Stedenbouw: Geen rechterlijke herstelmaatregel of bestuurlijke maatregel opgelegd/geen voorkooprecht")]
    [InlineData("Gebouw vergund geacht.")]
    public void Detect_ReturnsCompliant_WhenTheListingClaimsEverythingIsInOrder(string text) =>
        Assert.Equal(UrbanisticStatus.Compliant, UrbanisticStatusDetector.Detect(text));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Immeuble de rapport à vendre à Laeken, 1020 16 chambres · 718 m² € 1.585.000")]
    [InlineData("Urbanisme : zone d’habitation, entreprises artisanales et PME")]
    [InlineData("Permis d’urbanisme : pas de permis connu")]
    [InlineData("Contrôle électricité : pas conforme")]
    [InlineData("Herstelvordering Vlaamse Codex Wonen")]
    // "reconnus" on its own is not a claim: here the seller says the count is NOT confirmed.
    [InlineData("Plusieurs cuisines sont mentionnées sans que le nombre exact de logements reconnus ne soit confirmé.")]
    public void Detect_ReturnsUnknown_WhenTheTextSaysNothingUsable(string? text) =>
        Assert.Equal(UrbanisticStatus.Unknown, UrbanisticStatusDetector.Detect(text));

    [Fact]
    public void Detect_PrefersInfraction_WhenBothReadingsArePresent() =>
        Assert.Equal(
            UrbanisticStatus.Infraction,
            UrbanisticStatusDetector.Detect("Pas d'infraction au rez, mais le studio du 3e est à régulariser."));
}
