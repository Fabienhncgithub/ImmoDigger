using ImmoDigger.Application.Common;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImmoDigger.Infrastructure.BackgroundServices;

/// <summary>
/// Periodically runs every enabled, due <see cref="ListingSource"/> through
/// its matching <see cref="IListingCollector"/>. Ticks every
/// <see cref="CollectionOptions.DefaultPollingIntervalMinutes"/>; within a
/// tick, a source only actually runs if its own
/// <see cref="ListingSource.PollingIntervalMinutes"/> has elapsed since its
/// last successful run, so sources can be configured with a slower cadence
/// than the service's check interval.
///
/// <see cref="RunCollectionCycleAsync"/> is public so it can also be
/// triggered on demand (e.g. by the "POST /api/collection/run" endpoint
/// added in a later commit), not just by the timer loop.
/// </summary>
public class ListingCollectionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<CollectionOptions> options,
    ILogger<ListingCollectionBackgroundService> logger) : BackgroundService
{
    private readonly CollectionOptions _options = options.Value;

    // Prevents two collection cycles from running at the same time (e.g. if
    // one cycle takes longer than the tick interval). A cycle that finds the
    // lock held simply skips itself rather than queuing up.
    private readonly SemaphoreSlim _cycleLock = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ScheduleEnabled)
        {
            logger.LogInformation("Scheduled listing collection is disabled; manual collection remains available.");
            return;
        }

        logger.LogInformation(
            "Listing collection background service started (tick every {IntervalMinutes} min, max {MaxConcurrent} concurrent collectors).",
            _options.DefaultPollingIntervalMinutes,
            _options.MaxConcurrentCollectors);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _options.DefaultPollingIntervalMinutes)));

        try
        {
            // Run once at startup so newly-enabled sources don't wait a full
            // tick interval before their first collection.
            await RunCollectionCycleAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCollectionCycleAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Listing collection background service stopping.");
        }
    }

    public async Task RunCollectionCycleAsync(CancellationToken cancellationToken, bool force = false)
    {
        if (!await _cycleLock.WaitAsync(0, cancellationToken))
        {
            logger.LogWarning("A collection cycle is already running; skipping this tick.");
            return;
        }

        try
        {
            List<ListingSource> dueSources;
            using (var scope = scopeFactory.CreateScope())
            {
                var sourceRepository = scope.ServiceProvider.GetRequiredService<IListingSourceRepository>();
                var enabledSources = await sourceRepository.GetEnabledAsync(cancellationToken);
                var now = DateTime.UtcNow;
                dueSources = force
                    ? enabledSources.ToList()
                    : enabledSources.Where(source => IsDue(source, now)).ToList();
            }

            if (dueSources.Count == 0)
            {
                logger.LogInformation("Collection cycle: no source due for collection.");
                return;
            }

            using var limiter = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrentCollectors));
            var tasks = dueSources.Select(source => RunSingleSourceAsync(source.Id, limiter, cancellationToken));
            await Task.WhenAll(tasks);
        }
        finally
        {
            _cycleLock.Release();
        }
    }

    private async Task RunSingleSourceAsync(Guid sourceId, SemaphoreSlim limiter, CancellationToken cancellationToken)
    {
        await limiter.WaitAsync(cancellationToken);
        try
        {
            // Each source gets its own DI scope (and therefore its own
            // DbContext) so sources can run in parallel without sharing a
            // non-thread-safe EF Core context.
            using var scope = scopeFactory.CreateScope();
            var sourceRepository = scope.ServiceProvider.GetRequiredService<IListingSourceRepository>();
            var deduplicationService = scope.ServiceProvider.GetRequiredService<IListingDeduplicationService>();
            var analysisService = scope.ServiceProvider.GetRequiredService<IInvestmentAnalysisService>();
            var collectors = scope.ServiceProvider.GetServices<IListingCollector>();

            var source = await sourceRepository.GetByIdAsync(sourceId, cancellationToken);
            if (source is null || !source.IsEnabled)
            {
                return;
            }

            // Hard compliance gate, independent of whatever an individual
            // collector implementation does or doesn't check itself: a
            // source explicitly marked not-Allowed never runs, period.
            if (!source.Allowed)
            {
                logger.LogWarning(
                    "Source {SourceName} is enabled but not Allowed (compliance gate) - skipping.", source.Name);
                return;
            }

            var collector = collectors.FirstOrDefault(
                c => string.Equals(c.SourceName, source.Name, StringComparison.OrdinalIgnoreCase));

            if (collector is null)
            {
                logger.LogWarning(
                    "Source {SourceName} is enabled but no matching collector is registered.", source.Name);
                return;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.CollectorTimeout);

            try
            {
                var collected = await collector.CollectAsync(timeoutCts.Token);

                int newCount = 0, updatedCount = 0, unchangedCount = 0, duplicateCount = 0;

                foreach (var item in collected)
                {
                    var outcome = await deduplicationService.ProcessAsync(item, cancellationToken);

                    // Index/risk are computed right away rather than only
                    // on demand from the UI: otherwise every freshly-
                    // collected listing would sit without an assessment
                    // until a user happened to open it manually,
                    // which defeats the point of surfacing opportunities
                    // automatically. Re-run on every update too (not just
                    // "new"), since a price or PEB change can change the
                    // outcome. For an unchanged listing, a missing risk level
                    // is the marker that analysis has never run. A null index
                    // alone is not such a marker anymore: it can legitimately
                    // mean the listing lacks enough data. EstimatedGrossYield
                    // still depends on
                    // manual rent-estimate inputs the user hasn't filled in
                    // yet, so it stays null until they do - only the risk
                    // assessment and comparison index benefit here.
                    var needsAnalysis = outcome.Result is DeduplicationResult.NewListing or DeduplicationResult.ExistingListingUpdated
                        || (outcome.Result == DeduplicationResult.Unchanged && outcome.Listing?.RiskLevel is null);
                    if (outcome.Listing is not null && needsAnalysis)
                    {
                        analysisService.Analyze(outcome.Listing);
                    }

                    switch (outcome.Result)
                    {
                        case DeduplicationResult.NewListing:
                            newCount++;
                            break;
                        case DeduplicationResult.ExistingListingUpdated:
                            updatedCount++;
                            break;
                        case DeduplicationResult.Unchanged:
                            unchangedCount++;
                            break;
                        case DeduplicationResult.ProbableDuplicate:
                            duplicateCount++;
                            logger.LogWarning(
                                "Collector {SourceName}: probable duplicate not imported ({Reasons})",
                                source.Name, string.Join(" ", outcome.Reasons));
                            break;
                    }
                }

                logger.LogInformation(
                    "Collector {SourceName} found {Count} listing(s): {New} new, {Updated} updated, " +
                    "{Unchanged} unchanged, {Duplicate} probable duplicate(s).",
                    source.Name, collected.Count, newCount, updatedCount, unchangedCount, duplicateCount);

                source.LastSuccessfulRunAt = DateTime.UtcNow;
                source.LastError = null;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Collector {SourceName} timed out after {Timeout}.", source.Name, _options.CollectorTimeout);
                source.LastFailedRunAt = DateTime.UtcNow;
                source.LastError = $"Timed out after {_options.CollectorTimeout}.";
            }
            catch (Exception ex)
            {
                // A single failing collector must never take down the
                // others: the exception is caught, logged, and recorded as
                // diagnostic information on the source itself.
                logger.LogError(ex, "Collector {SourceName} failed.", source.Name);
                source.LastFailedRunAt = DateTime.UtcNow;
                source.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            }

            sourceRepository.Update(source);

            // Both repositories resolved from this scope share the same
            // DbContext, so this single call also persists every listing
            // added/updated by the deduplication service above.
            await sourceRepository.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            limiter.Release();
        }
    }

    private static bool IsDue(ListingSource source, DateTime now) =>
        source.LastSuccessfulRunAt is null ||
        now - source.LastSuccessfulRunAt >= TimeSpan.FromMinutes(Math.Max(1, source.PollingIntervalMinutes));
}
