using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence;

/// <summary>
/// Brings stored listings in line with <see cref="UrbanisticStatusDetector"/>
/// at startup. Collection only re-analyses a listing when its content
/// changes, so without this, listings collected before the detector existed
/// (or before its wording list was last extended) would keep a stale status.
/// </summary>
public static class UrbanisticStatusBackfill
{
    public static async Task<int> RunAsync(
        ImmoDiggerDbContext dbContext,
        IInvestmentAnalysisService analysisService,
        CancellationToken cancellationToken = default)
    {
        var listings = await dbContext.PropertyListings.ToListAsync(cancellationToken);
        var updated = 0;

        foreach (var listing in listings)
        {
            if (listing.UrbanisticStatus == UrbanisticStatusDetector.Detect(listing))
            {
                continue;
            }

            // The status feeds the risk assessment and the index, so the
            // whole analysis is redone rather than the status alone.
            analysisService.Analyze(listing);
            updated++;
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return updated;
    }
}
