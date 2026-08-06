using ImmoDigger.Application.Common;
using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.DTOs;

/// <summary>
/// Result of running a <see cref="DTOs.CollectedListing"/> through
/// <see cref="Interfaces.IListingDeduplicationService"/>.
/// <see cref="Listing"/> is the persisted (new or updated) entity for
/// <see cref="DeduplicationResult.NewListing"/>,
/// <see cref="DeduplicationResult.ExistingListingUpdated"/> and
/// <see cref="DeduplicationResult.Unchanged"/>; it is <see langword="null"/>
/// for <see cref="DeduplicationResult.ProbableDuplicate"/>, since nothing
/// is written automatically - <see cref="Reasons"/> explains why it was
/// flagged, for manual review.
/// </summary>
public sealed record DeduplicationOutcome(
    DeduplicationResult Result,
    PropertyListing? Listing,
    IReadOnlyCollection<string> Reasons)
{
    public static DeduplicationOutcome New(PropertyListing listing) =>
        new(DeduplicationResult.NewListing, listing, []);

    public static DeduplicationOutcome Updated(PropertyListing listing, IReadOnlyCollection<string> reasons) =>
        new(DeduplicationResult.ExistingListingUpdated, listing, reasons);

    public static DeduplicationOutcome Unchanged(PropertyListing listing) =>
        new(DeduplicationResult.Unchanged, listing, []);

    public static DeduplicationOutcome Duplicate(IReadOnlyCollection<string> reasons) =>
        new(DeduplicationResult.ProbableDuplicate, null, reasons);
}
