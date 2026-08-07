using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Services;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using ImmoDigger.Tests.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImmoDigger.Tests.Application.Services;

public class EmailListingImportServiceTests
{
    private sealed class FakeEmailInbox(params EmailMessage[] messages) : IEmailInbox
    {
        public Task<IReadOnlyCollection<EmailMessage>> FetchNewMessagesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<EmailMessage>>(messages);
    }

    private sealed class FakeParser(string sourceName, string senderDomain, int listingsPerMessage = 1) : IEmailListingParser
    {
        public List<EmailMessage> ParsedMessages { get; } = [];

        public string SourceName => sourceName;

        public bool CanParse(string senderAddress) => senderAddress.Contains(senderDomain, StringComparison.OrdinalIgnoreCase);

        public IReadOnlyCollection<CollectedListing> Parse(EmailMessage message)
        {
            ParsedMessages.Add(message);
            return Enumerable.Range(1, listingsPerMessage)
                .Select(i => new CollectedListing
                {
                    Source = sourceName,
                    ExternalId = $"{message.MessageId}-{i}",
                    Url = $"https://example.invalid/{sourceName}/{i}",
                    Title = $"Listing {i} from {sourceName}",
                    SaleType = "RegularSale",
                    PropertyType = "House",
                    RawContentHash = $"hash-{message.MessageId}-{i}",
                })
                .ToList();
        }
    }

    private static EmailMessage CreateMessage(string id, string sender) =>
        new() { MessageId = id, Sender = sender, Subject = "Alert", ReceivedAt = DateTime.UtcNow };

    [Fact]
    public async Task ImportAsync_RoutesEachMessageToTheParserThatRecognizesItsSender()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var processedRepository = new ProcessedEmailMessageRepository(dbContext);
        var immowebParser = new FakeParser("Immoweb", "immoweb.be");
        var immovlanParser = new FakeParser("Immovlan", "immovlan.be");

        var sut = new EmailListingImportService(
            new FakeEmailInbox(
                CreateMessage("<1@immoweb.be>", "alerts@immoweb.be"),
                CreateMessage("<2@immovlan.be>", "alerts@immovlan.be")),
            [immowebParser, immovlanParser],
            processedRepository,
            NullLogger<EmailListingImportService>.Instance);

        var results = await sut.ImportAsync(CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Single(immowebParser.ParsedMessages);
        Assert.Single(immovlanParser.ParsedMessages);
    }

    [Fact]
    public async Task ImportAsync_SkipsMessagesWithNoRecognizedParser_ButStillMarksThemProcessed()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var processedRepository = new ProcessedEmailMessageRepository(dbContext);
        var sut = new EmailListingImportService(
            new FakeEmailInbox(CreateMessage("<1@unknown.example>", "newsletter@unknown.example")),
            [],
            processedRepository,
            NullLogger<EmailListingImportService>.Instance);

        var results = await sut.ImportAsync(CancellationToken.None);

        Assert.Empty(results);
        Assert.True(await processedRepository.IsProcessedAsync("<1@unknown.example>", CancellationToken.None));
    }

    [Fact]
    public async Task ImportAsync_NeverProcessesTheSameMessageTwice()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var processedRepository = new ProcessedEmailMessageRepository(dbContext);
        var parser = new FakeParser("Immoweb", "immoweb.be");
        var message = CreateMessage("<1@immoweb.be>", "alerts@immoweb.be");

        // First run processes it normally.
        await new EmailListingImportService(
            new FakeEmailInbox(message), [parser], processedRepository, NullLogger<EmailListingImportService>.Instance)
            .ImportAsync(CancellationToken.None);

        // A second run sees the same message again (e.g. inbox re-sync)
        // but must not import it - or call the parser - a second time.
        var secondResults = await new EmailListingImportService(
            new FakeEmailInbox(message), [parser], processedRepository, NullLogger<EmailListingImportService>.Instance)
            .ImportAsync(CancellationToken.None);

        Assert.Empty(secondResults);
        Assert.Single(parser.ParsedMessages);
    }
}
