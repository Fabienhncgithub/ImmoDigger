namespace ImmoDigger.Infrastructure.BackgroundServices;

/// <summary>
/// Binds the "Collection" appsettings section. <see cref="DefaultPollingIntervalMinutes"/>
/// and <see cref="MaxConcurrentCollectors"/> come directly from the product
/// brief's example; <see cref="CollectorTimeoutSeconds"/> is an addition
/// needed to bound each collector call ("gérer les timeouts") and isn't in
/// the original example.
/// </summary>
public class CollectionOptions
{
    public const string SectionName = "Collection";

    /// <summary>
    /// Controls the periodic scheduler only. The explicit API trigger stays
    /// available, which is useful for a deterministic demo/test startup.
    /// </summary>
    public bool ScheduleEnabled { get; set; } = true;

    public int DefaultPollingIntervalMinutes { get; set; } = 15;

    public int MaxConcurrentCollectors { get; set; } = 2;

    public int CollectorTimeoutSeconds { get; set; } = 60;

    public TimeSpan CollectorTimeout => TimeSpan.FromSeconds(Math.Max(1, CollectorTimeoutSeconds));
}
