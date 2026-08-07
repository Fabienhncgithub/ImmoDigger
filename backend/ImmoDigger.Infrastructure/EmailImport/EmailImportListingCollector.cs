using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;

namespace ImmoDigger.Infrastructure.EmailImport;

/// <summary>
/// Adapts <see cref="IEmailListingImporter"/> to <see cref="IListingCollector"/>
/// so email-derived listings flow through the exact same deduplication,
/// analysis and scoring pipeline as every other source, gated by the same
/// "EmailImport" <see cref="Domain.Entities.ListingSource"/> row (enable/
/// disable, polling interval) as any other collector.
/// </summary>
public class EmailImportListingCollector(IEmailListingImporter importer) : IListingCollector
{
    public string SourceName => "EmailImport";

    public Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken) =>
        importer.ImportAsync(cancellationToken);
}
