using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Services;
using ImmoDigger.Domain.Entities;
using ImmoDigger.Infrastructure.BackgroundServices;
using ImmoDigger.Infrastructure.Persistence;
using ImmoDigger.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ImmoDigger.Tests.Infrastructure.BackgroundServices;

public class ListingCollectionBackgroundServiceTests
{
    private static ServiceProvider BuildProvider(
        IEnumerable<IListingCollector> collectors,
        CollectionOptions? options = null)
    {
        var services = new ServiceCollection();

        // The database name must be captured once outside the options
        // delegate: AddDbContext re-invokes it for every new scope, so
        // calling Guid.NewGuid() inline would give each scope its own
        // (empty) InMemory database instead of a shared one.
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<ImmoDiggerDbContext>(o => o.UseInMemoryDatabase(databaseName));
        services.AddScoped<IListingSourceRepository, ListingSourceRepository>();
        services.AddScoped<IPropertyListingRepository, PropertyListingRepository>();
        services.AddScoped<IListingDeduplicationService, ListingDeduplicationService>();

        foreach (var collector in collectors)
        {
            services.AddSingleton(collector);
        }

        services.AddSingleton<IOptions<CollectionOptions>>(
            Options.Create(options ?? new CollectionOptions { CollectorTimeoutSeconds = 5 }));

        return services.BuildServiceProvider();
    }

    private static ListingCollectionBackgroundService CreateSut(ServiceProvider provider, CollectionOptions? options = null) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<CollectionOptions>>(),
            NullLogger<ListingCollectionBackgroundService>.Instance);

    private static async Task<ListingSource> SeedSourceAsync(
        ServiceProvider provider,
        string name,
        bool isEnabled = true,
        DateTime? lastSuccessfulRunAt = null,
        int pollingIntervalMinutes = 15)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ImmoDiggerDbContext>();

        var source = new ListingSource
        {
            Name = name,
            BaseUrl = "https://example.invalid",
            IsEnabled = isEnabled,
            PollingIntervalMinutes = pollingIntervalMinutes,
            LastSuccessfulRunAt = lastSuccessfulRunAt,
        };

        dbContext.ListingSources.Add(source);
        await dbContext.SaveChangesAsync();
        return source;
    }

    private static async Task<ListingSource> ReloadAsync(ServiceProvider provider, Guid id)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ImmoDiggerDbContext>();
        return await dbContext.ListingSources.SingleAsync(s => s.Id == id);
    }

    [Fact]
    public async Task RunCollectionCycleAsync_RunsCollector_ForDueEnabledSource()
    {
        var collector = new RecordingTestCollector("Test");
        using var provider = BuildProvider([collector]);
        var source = await SeedSourceAsync(provider, "Test");
        var sut = CreateSut(provider);

        await sut.RunCollectionCycleAsync(CancellationToken.None);

        Assert.Equal(1, collector.InvocationCount);
        var reloaded = await ReloadAsync(provider, source.Id);
        Assert.NotNull(reloaded.LastSuccessfulRunAt);
        Assert.Null(reloaded.LastError);
    }

    [Fact]
    public async Task RunCollectionCycleAsync_SkipsDisabledSources()
    {
        var collector = new RecordingTestCollector("Disabled");
        using var provider = BuildProvider([collector]);
        await SeedSourceAsync(provider, "Disabled", isEnabled: false);
        var sut = CreateSut(provider);

        await sut.RunCollectionCycleAsync(CancellationToken.None);

        Assert.Equal(0, collector.InvocationCount);
    }

    [Fact]
    public async Task RunCollectionCycleAsync_SkipsSourcesNotYetDue()
    {
        var collector = new RecordingTestCollector("NotDue");
        using var provider = BuildProvider([collector]);
        await SeedSourceAsync(provider, "NotDue", lastSuccessfulRunAt: DateTime.UtcNow, pollingIntervalMinutes: 15);
        var sut = CreateSut(provider);

        await sut.RunCollectionCycleAsync(CancellationToken.None);

        Assert.Equal(0, collector.InvocationCount);
    }

    [Fact]
    public async Task RunCollectionCycleAsync_IsolatesAFailingCollector_FromOtherSources()
    {
        var failing = new ThrowingTestCollector("Failing");
        var working = new RecordingTestCollector("Working");
        using var provider = BuildProvider([failing, working]);
        var failingSource = await SeedSourceAsync(provider, "Failing");
        var workingSource = await SeedSourceAsync(provider, "Working");
        var sut = CreateSut(provider);

        await sut.RunCollectionCycleAsync(CancellationToken.None);

        Assert.Equal(1, working.InvocationCount);

        var reloadedFailing = await ReloadAsync(provider, failingSource.Id);
        Assert.NotNull(reloadedFailing.LastFailedRunAt);
        Assert.Contains("Simulated collector failure", reloadedFailing.LastError);

        var reloadedWorking = await ReloadAsync(provider, workingSource.Id);
        Assert.NotNull(reloadedWorking.LastSuccessfulRunAt);
    }

    [Fact]
    public async Task RunCollectionCycleAsync_DoesNotThrow_WhenNoCollectorIsRegisteredForAnEnabledSource()
    {
        using var provider = BuildProvider([]);
        var source = await SeedSourceAsync(provider, "NoCollector");
        var sut = CreateSut(provider);

        await sut.RunCollectionCycleAsync(CancellationToken.None);

        var reloaded = await ReloadAsync(provider, source.Id);
        Assert.Null(reloaded.LastSuccessfulRunAt);
        Assert.Null(reloaded.LastFailedRunAt);
    }

    [Fact]
    public async Task RunCollectionCycleAsync_SkipsASecondCall_WhileACycleIsStillRunning()
    {
        var gated = new GatedTestCollector("Gated");
        using var provider = BuildProvider([gated]);
        await SeedSourceAsync(provider, "Gated");
        var sut = CreateSut(provider);

        var firstCycle = sut.RunCollectionCycleAsync(CancellationToken.None);

        // Give the first cycle time to acquire the lock and start the collector.
        await Task.Delay(50);

        // The second call must see the lock held and return immediately
        // instead of waiting for the first cycle to finish.
        await sut.RunCollectionCycleAsync(CancellationToken.None);

        gated.Release();
        await firstCycle;

        Assert.Equal(1, gated.InvocationCount);
    }

    [Fact]
    public async Task RunCollectionCycleAsync_RespectsMaxConcurrentCollectors()
    {
        var probe = new ConcurrencyProbe();
        var delay = TimeSpan.FromMilliseconds(150);
        var collectors = new IListingCollector[]
        {
            new TrackingTestCollector("A", probe, delay),
            new TrackingTestCollector("B", probe, delay),
            new TrackingTestCollector("C", probe, delay),
        };

        using var provider = BuildProvider(collectors, new CollectionOptions { MaxConcurrentCollectors = 1, CollectorTimeoutSeconds = 5 });
        await SeedSourceAsync(provider, "A");
        await SeedSourceAsync(provider, "B");
        await SeedSourceAsync(provider, "C");
        var sut = CreateSut(provider);

        await sut.RunCollectionCycleAsync(CancellationToken.None);

        Assert.Equal(1, probe.Peak);
    }
}
