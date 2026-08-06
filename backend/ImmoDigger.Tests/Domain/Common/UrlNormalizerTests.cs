using ImmoDigger.Domain.Common;

namespace ImmoDigger.Tests.Domain.Common;

public class UrlNormalizerTests
{
    [Theory]
    [InlineData("https://www.immoweb.be/listing/1", "https://immoweb.be/listing/1")]
    [InlineData("https://immoweb.be/listing/1/", "https://immoweb.be/listing/1")]
    [InlineData("https://IMMOWEB.be/Listing/1", "https://immoweb.be/listing/1")]
    [InlineData("https://immoweb.be/listing/1?utm_source=x", "https://immoweb.be/listing/1")]
    public void Normalize_ProducesTheSameKey_ForEquivalentUrls(string first, string second)
    {
        Assert.Equal(UrlNormalizer.Normalize(first), UrlNormalizer.Normalize(second));
    }

    [Fact]
    public void Normalize_DifferentPaths_ProduceDifferentKeys()
    {
        var a = UrlNormalizer.Normalize("https://immoweb.be/listing/1");
        var b = UrlNormalizer.Normalize("https://immoweb.be/listing/2");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Normalize_ReturnsEmptyString_ForNullOrWhitespace()
    {
        Assert.Equal(string.Empty, UrlNormalizer.Normalize(null));
        Assert.Equal(string.Empty, UrlNormalizer.Normalize("   "));
    }
}
