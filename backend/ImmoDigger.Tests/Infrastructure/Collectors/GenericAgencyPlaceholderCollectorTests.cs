using ImmoDigger.Infrastructure.Collectors;

namespace ImmoDigger.Tests.Infrastructure.Collectors;

public class GenericAgencyPlaceholderCollectorTests
{
    [Fact]
    public void SourceName_MatchesTheGenericAgencySeededSource()
    {
        var collector = new GenericAgencyPlaceholderCollector();

        Assert.Equal("GenericAgency", collector.SourceName);
    }

    [Fact]
    public async Task CollectAsync_ReturnsAtLeastOneFictionalListing()
    {
        var collector = new GenericAgencyPlaceholderCollector();

        var results = await collector.CollectAsync(CancellationToken.None);

        Assert.NotEmpty(results);
        Assert.All(results, listing =>
        {
            Assert.Equal("GenericAgency", listing.Source);
            Assert.False(string.IsNullOrWhiteSpace(listing.ExternalId));
            Assert.False(string.IsNullOrWhiteSpace(listing.RawContentHash));
        });
    }
}
