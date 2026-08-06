using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence;

public class ImmoDiggerDbContext(DbContextOptions<ImmoDiggerDbContext> options) : DbContext(options)
{
    public DbSet<PropertyListing> PropertyListings => Set<PropertyListing>();

    public DbSet<ListingPriceHistory> ListingPriceHistories => Set<ListingPriceHistory>();

    public DbSet<ListingSource> ListingSources => Set<ListingSource>();

    public DbSet<SearchProfile> SearchProfiles => Set<SearchProfile>();

    public DbSet<NotificationHistory> NotificationHistories => Set<NotificationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ImmoDiggerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
