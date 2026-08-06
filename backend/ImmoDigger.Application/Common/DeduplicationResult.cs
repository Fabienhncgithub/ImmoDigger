namespace ImmoDigger.Application.Common;

public enum DeduplicationResult
{
    NewListing,
    ExistingListingUpdated,
    ProbableDuplicate,
    Unchanged,
}
