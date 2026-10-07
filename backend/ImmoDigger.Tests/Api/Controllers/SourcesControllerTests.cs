using ImmoDigger.Api.Controllers;
using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Entities;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using ImmoDigger.Tests.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ImmoDigger.Tests.Api.Controllers;

public class SourcesControllerTests
{
    private sealed class EmptyInbox : IEmailInbox
    {
        public Task<IReadOnlyCollection<EmailMessage>> FetchNewMessagesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<EmailMessage>>([]);
    }

    private static SourcesController CreateController(
        ListingSourceRepository sourceRepository,
        PropertyListingRepository listingRepository,
        IEnumerable<IEmailListingParser>? parsers = null,
        IDictionary<string, string?>? configuration = null) =>
        new(
            sourceRepository,
            listingRepository,
            parsers ?? [],
            new EmptyInbox(),
            new ConfigurationBuilder().AddInMemoryCollection(configuration).Build());

    [Fact]
    public async Task GetAll_ReturnsSourcesWithListingCounts()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var sourceRepository = new ListingSourceRepository(dbContext);
        var listingRepository = new PropertyListingRepository(dbContext);
        await sourceRepository.AddAsync(new ListingSource { Name = "Immoweb", BaseUrl = "https://immoweb.be" });
        await listingRepository.AddAsync(new PropertyListing
        {
            Source = "Immoweb", ExternalId = "1", Url = "https://immoweb.be/1", Title = "T",
            SaleType = "RegularSale", PropertyType = "IncomeBuilding", RawContentHash = "h",
            FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
        });
        await listingRepository.AddAsync(new PropertyListing
        {
            Source = "Immoweb", ExternalId = "DEMO-1", Url = "https://example.invalid/demo", Title = "Demo",
            SaleType = "RegularSale", PropertyType = "IncomeBuilding", RawContentHash = "demo",
            FirstSeenAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
        });
        await sourceRepository.SaveChangesAsync();
        var controller = CreateController(sourceRepository, listingRepository);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IReadOnlyList<SourceDto>>(ok.Value);
        Assert.Equal(1, dtos.Single(d => d.Name == "Immoweb").ListingCount);
    }

    [Fact]
    public async Task Update_TogglesActivation()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var sourceRepository = new ListingSourceRepository(dbContext);
        var listingRepository = new PropertyListingRepository(dbContext);
        var source = new ListingSource { Name = "Immoweb", BaseUrl = "https://immoweb.be", IsEnabled = true };
        await sourceRepository.AddAsync(source);
        await sourceRepository.SaveChangesAsync();
        var controller = CreateController(sourceRepository, listingRepository);

        var result = await controller.Update(
            source.Id, new UpdateSourceRequest(IsEnabled: false, PollingIntervalMinutes: 30), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SourceDto>(ok.Value);
        Assert.False(dto.IsEnabled);
        Assert.Equal(30, dto.PollingIntervalMinutes);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_ForUnknownId()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(
            new ListingSourceRepository(dbContext), new PropertyListingRepository(dbContext));

        var result = await controller.Update(
            Guid.NewGuid(), new UpdateSourceRequest(true, null), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetEmailImportStatus_DoesNotExposeCredentials_AndListsMissingSettings()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(
            new ListingSourceRepository(dbContext),
            new PropertyListingRepository(dbContext),
            configuration: new Dictionary<string, string?> { ["Imap:Host"] = "imap.example.test" });

        var result = controller.GetEmailImportStatus();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var status = Assert.IsType<EmailImportStatusDto>(ok.Value);
        Assert.False(status.IsConfigured);
        Assert.Contains("IMAP_USERNAME manquant", status.ConfigurationIssues);
        Assert.Contains("IMAP_PASSWORD manquant", status.ConfigurationIssues);
        Assert.DoesNotContain("imap.example.test", string.Join(' ', status.ConfigurationIssues));
    }

    [Fact]
    public async Task TestEmailImport_RejectsIncompleteConfiguration()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var controller = CreateController(
            new ListingSourceRepository(dbContext),
            new PropertyListingRepository(dbContext));

        var result = await controller.TestEmailImport(CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var status = Assert.IsType<EmailImportConnectionTestDto>(badRequest.Value);
        Assert.False(status.Success);
    }
}
