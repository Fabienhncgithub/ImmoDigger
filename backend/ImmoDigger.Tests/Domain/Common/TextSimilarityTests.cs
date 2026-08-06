using ImmoDigger.Domain.Common;

namespace ImmoDigger.Tests.Domain.Common;

public class TextSimilarityTests
{
    [Fact]
    public void WordOverlapRatio_IsOne_ForIdenticalText()
    {
        var ratio = TextSimilarity.WordOverlapRatio(
            "Immeuble de rapport Avenue Coghen",
            "Immeuble de rapport Avenue Coghen");

        Assert.Equal(1, ratio);
    }

    [Fact]
    public void WordOverlapRatio_IsZero_ForCompletelyDifferentText()
    {
        var ratio = TextSimilarity.WordOverlapRatio(
            "Immeuble de rapport Avenue Coghen",
            "Studio meuble centre ville");

        Assert.Equal(0, ratio);
    }

    [Fact]
    public void WordOverlapRatio_IsBetweenZeroAndOne_ForPartiallyOverlappingText()
    {
        var ratio = TextSimilarity.WordOverlapRatio(
            "Maison de rapport Avenue Coghen Uccle",
            "Maison de rapport Avenue Coghen renovee");

        Assert.InRange(ratio, 0.4, 0.9);
    }

    [Fact]
    public void WordOverlapRatio_IsZero_WhenEitherTextIsEmpty()
    {
        Assert.Equal(0, TextSimilarity.WordOverlapRatio("", "Immeuble"));
        Assert.Equal(0, TextSimilarity.WordOverlapRatio("Immeuble", ""));
    }
}
