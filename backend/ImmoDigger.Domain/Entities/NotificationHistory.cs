namespace ImmoDigger.Domain.Entities;

/// <summary>
/// Record of a notification sent (or attempted) for a listing. Used to
/// avoid notifying the same listing twice on the same channel.
/// </summary>
public class NotificationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PropertyListingId { get; set; }

    public PropertyListing? PropertyListing { get; set; }

    /// <summary>e.g. "Telegram", "Email".</summary>
    public required string Channel { get; set; }

    public DateTime SentAt { get; set; }

    /// <summary>e.g. "Sent", "Failed".</summary>
    public required string Status { get; set; }

    public string? Error { get; set; }
}
