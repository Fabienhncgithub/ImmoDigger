using System.Globalization;
using System.Text;

namespace ImmoDigger.Domain.Common;

/// <summary>
/// Normalizes a Belgian street address into a stable comparison key:
/// lowercase, accents stripped, punctuation removed, common street-type
/// abbreviations expanded (av. -> avenue, ch. -> chaussee, ...), combined
/// with postal code and city. Used for deduplication level 3 (normalized
/// address match).
/// </summary>
public static class AddressNormalizer
{
    private static readonly Dictionary<string, string> AbbreviationExpansions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["av"] = "avenue",
        ["ave"] = "avenue",
        ["bd"] = "boulevard",
        ["boul"] = "boulevard",
        ["ch"] = "chaussee",
        ["chee"] = "chaussee",
        ["chaus"] = "chaussee",
        ["r"] = "rue",
        ["pl"] = "place",
        ["sq"] = "square",
        ["all"] = "allee",
        ["imp"] = "impasse",
        ["st"] = "sainte",
        ["ste"] = "sainte",
    };

    /// <summary>
    /// Returns a stable "postalCode|city|address" key. Two addresses that
    /// normalize to the same key are considered the same location.
    /// </summary>
    public static string Normalize(string? address, string? postalCode, string? city)
    {
        var normalizedPostalCode = (postalCode ?? string.Empty).Trim();
        var normalizedCity = NormalizeText(city);
        var normalizedAddress = NormalizeText(address);

        return $"{normalizedPostalCode}|{normalizedCity}|{normalizedAddress}";
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutAccents = RemoveDiacritics(value.ToLowerInvariant());

        // Keep only letters, digits and spaces; anything else (commas,
        // periods, dashes, ...) becomes a space so tokens split cleanly.
        var cleaned = new string(withoutAccents
            .Select(c => char.IsLetterOrDigit(c) || c == ' ' ? c : ' ')
            .ToArray());

        var words = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => AbbreviationExpansions.TryGetValue(word, out var expanded) ? expanded : word);

        return string.Join(' ', words);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
