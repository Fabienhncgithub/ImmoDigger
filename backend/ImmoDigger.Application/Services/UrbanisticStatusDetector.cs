using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ImmoDigger.Domain.Common;
using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Services;

/// <summary>
/// Reads a listing's title and description (French and Dutch) and
/// classifies what the seller says about the planning situation. This is a
/// keyword heuristic over the text the source gave us, not a legal check:
/// "Compliant" means "the listing claims so", never "verified".
/// </summary>
public static partial class UrbanisticStatusDetector
{
    public static string Detect(PropertyListing listing) =>
        Detect($"{listing.Title}\n{listing.Description}");

    public static string Detect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return UrbanisticStatus.Unknown;
        }

        var normalized = Normalize(text);

        // "pas d'infraction urbanistique" contains "infraction urbanistique":
        // negated mentions are taken out first so they can't be read as an
        // infraction afterwards.
        var withoutDenials = DeniedInfraction().Replace(normalized, " ");
        var deniesInfraction = withoutDenials.Length != normalized.Length;

        // An infraction wins over any reassuring wording elsewhere in the
        // same text: the cautious reading is the useful one here.
        if (Infraction().IsMatch(withoutDenials))
        {
            return UrbanisticStatus.Infraction;
        }

        return deniesInfraction || InOrder().IsMatch(withoutDenials)
            ? UrbanisticStatus.Compliant
            : UrbanisticStatus.Unknown;
    }

    /// <summary>Lower-cases, strips accents and flattens apostrophes/whitespace so one pattern covers every spelling.</summary>
    private static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character switch
            {
                '’' or '‘' or '`' or '´' => '\'',
                _ when char.IsWhiteSpace(character) => ' ',
                _ => character,
            });
        }

        return Whitespace().Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(
        @"\b(?:pas|aucune?|sans|ni|absence|exempte?s?)\s+(?:d'|de\s+|d\s+|en\s+)?(?:infraction|regularisation|irregularite)s?(?:\s+urbanistiques?)?" +
        @"|\binfractions?(?:\s+urbanistiques?)?\s*:?\s*(?:neant|aucune|non\b)" +
        @"|\bgeen\s+(?:enkele\s+)?(?:stedenbouwkundige?\s+)?(?:bouw)?(?:overtreding|inbreuk|misdrijf)\w*" +
        @"|\bgeen\s+dagvaarding\w*" +
        @"|\bgeen\s+rechterlijke\s+herstelmaatregel\w*" +
        @"|\bgeen\s+regularisatie\w*")]
    private static partial Regex DeniedInfraction();

    [GeneratedRegex(
        @"\binfractions?\b" +
        @"|\ba\s+regulariser\b|\bregulariser\b|\bregularisation\b|\bnon\s+regularise" +
        @"|\bpermis\s+(?:d'urbanisme\s+)?en\s+cours\b|\bdemande\s+de\s+permis\b" +
        @"|\bsans\s+permis\b" +
        @"|\b(?:non|pas)\s+reconnue?s?\b" +
        @"|\birregularite|\bsituation\s+(?:urbanistique\s+)?irreguliere" +
        @"|\bnon[-\s]conform\w*\s+(?:a\s+l'|aux?\s+|au\s+niveau\s+)?(?:\w+\s+)?urbanis" +
        @"|\bextension\s+non\s+documentee\b|\bnon\s+autorisee?s?\b" +
        @"|\bbouw(?:overtreding|misdrijf|inbreuk)\w*" +
        @"|\bstedenbouwkundige?\s+(?:overtreding|inbreuk|misdrijf)\w*" +
        @"|\bte\s+regulariseren\b|\bregularisatie\w*" +
        @"|\bdagvaarding\s+(?:werd\s+|is\s+)?uitgebracht" +
        @"|\bniet\s+vergund\b|\bonvergund\w*|\bzonder\s+vergunning\b")]
    private static partial Regex Infraction();

    [GeneratedRegex(
        @"\burbanis\w+\s*:?\s*(?:tout\s+)?(?:est\s+)?en\s+ordre\b" +
        @"|\ben\s+ordre\s+(?:au\s+niveau\s+|d'|de\s+l'|sur\s+le\s+plan\s+)?urbanis" +
        @"|\btout\s+(?:est\s+)?en\s+ordre\b" +
        @"|\b(?<!non\s)(?<!non-)conform\w*\s+(?:a\s+l'|aux?\s+|au\s+niveau\s+)?(?:\w+\s+)?urbanis" +
        @"|\burbanistiquement\s+(?:conforme|en\s+ordre|reconnu)" +
        @"|\bsituation\s+urbanistique\s+(?:reguliere|conforme|en\s+ordre)" +
        @"|\breconnue?s?\s+(?:a|par)\s+l'urbanisme\b|\b\d+\s+(?:logements?|unites?|appartements?|studios?)\s+reconnue?s?\b" +
        @"|\bregularisee?s?\b" +
        @"|\bpermis\s+(?:d'urbanisme\s+)?(?:delivre|obtenu|octroye)" +
        @"|\bvergund\s+geacht\b|\bvolledig\s+vergund\b|\bhoofdzakelijk\s+vergund\b|\bvergunde\s+toestand\b" +
        @"|\bstedenbouwkundig\s+in\s+orde\b|\bin\s+orde\s+met\s+stedenbouw")]
    private static partial Regex InOrder();
}
