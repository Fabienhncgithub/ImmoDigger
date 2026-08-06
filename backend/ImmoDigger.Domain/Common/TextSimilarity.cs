namespace ImmoDigger.Domain.Common;

/// <summary>
/// Lightweight text comparison used for deduplication level 4 (title
/// comparison): a word-overlap (Jaccard) ratio. Deliberately simple rather
/// than a full edit-distance algorithm - it is easy to reason about, fast,
/// and good enough to corroborate an address match.
/// </summary>
public static class TextSimilarity
{
    /// <summary>Returns a ratio between 0 (no shared words) and 1 (identical word sets).</summary>
    public static double WordOverlapRatio(string? a, string? b)
    {
        var wordsA = Tokenize(a);
        var wordsB = Tokenize(b);

        if (wordsA.Count == 0 || wordsB.Count == 0)
        {
            return 0;
        }

        var intersectionCount = wordsA.Intersect(wordsB).Count();
        var unionCount = wordsA.Union(wordsB).Count();

        return unionCount == 0 ? 0 : (double)intersectionCount / unionCount;
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .ToLowerInvariant()
            .Split([' ', '-', ',', '.', '\'', '"'], StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 2)
            .ToHashSet();
    }
}
