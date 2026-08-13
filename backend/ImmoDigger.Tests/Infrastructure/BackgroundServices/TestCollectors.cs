using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;

namespace ImmoDigger.Tests.Infrastructure.BackgroundServices;

/// <summary>Records how many times it was invoked; always succeeds with an empty result.</summary>
internal sealed class RecordingTestCollector(string sourceName) : IListingCollector
{
    public int InvocationCount { get; private set; }

    public string SourceName => sourceName;

    public Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken)
    {
        InvocationCount++;
        return Task.FromResult<IReadOnlyCollection<CollectedListing>>([]);
    }
}

/// <summary>Always returns the same single listing - used to test that the background service analyzes what it collects.</summary>
internal sealed class FixedResultTestCollector(string sourceName, CollectedListing listing) : IListingCollector
{
    public string SourceName => sourceName;

    public Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<CollectedListing>>([listing]);
}

/// <summary>Always throws, to test that the background service isolates collector failures.</summary>
internal sealed class ThrowingTestCollector(string sourceName) : IListingCollector
{
    public string SourceName => sourceName;

    public Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Simulated collector failure.");
}

/// <summary>Only completes when <see cref="Release"/> is called; used to test the cycle-level lock.</summary>
internal sealed class GatedTestCollector(string sourceName) : IListingCollector
{
    private readonly TaskCompletionSource _gate = new();

    public int InvocationCount { get; private set; }

    public string SourceName => sourceName;

    public void Release() => _gate.TrySetResult();

    public async Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken)
    {
        InvocationCount++;
        await _gate.Task;
        return [];
    }
}

/// <summary>Tracks how many instances are executing concurrently, to test MaxConcurrentCollectors.</summary>
internal sealed class ConcurrencyProbe
{
    private readonly object _lock = new();
    private int _current;

    public int Peak { get; private set; }

    public IDisposable Enter()
    {
        lock (_lock)
        {
            _current++;
            Peak = Math.Max(Peak, _current);
        }

        return new Exit(this);
    }

    private void Leave()
    {
        lock (_lock)
        {
            _current--;
        }
    }

    private sealed class Exit(ConcurrencyProbe probe) : IDisposable
    {
        public void Dispose() => probe.Leave();
    }
}

internal sealed class TrackingTestCollector(string sourceName, ConcurrencyProbe probe, TimeSpan delay) : IListingCollector
{
    public string SourceName => sourceName;

    public async Task<IReadOnlyCollection<CollectedListing>> CollectAsync(CancellationToken cancellationToken)
    {
        using (probe.Enter())
        {
            await Task.Delay(delay, cancellationToken);
        }

        return [];
    }
}
