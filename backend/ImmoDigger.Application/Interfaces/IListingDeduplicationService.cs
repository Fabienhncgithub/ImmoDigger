using ImmoDigger.Application.DTOs;

namespace ImmoDigger.Application.Interfaces;

/// <summary>
/// Decides whether a freshly collected listing is new, an update to an
/// existing listing, a probable duplicate of one, or unchanged - using
/// multiple deduplication levels (source+external id, normalized URL,
/// normalized address combined with title/price/area similarity, cleaned
/// content hash). Never deletes or silently merges a probable duplicate;
/// it is only flagged (via <see cref="DeduplicationOutcome.Reasons"/>) for
/// manual review.
///
/// Stages the resulting change on the underlying repository (add/update)
/// but does not call SaveChanges - the caller controls the transaction
/// boundary (e.g. one save per collection run).
/// </summary>
public interface IListingDeduplicationService
{
    Task<DeduplicationOutcome> ProcessAsync(CollectedListing collected, CancellationToken cancellationToken = default);
}
