using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence.Repositories;

public class ListingSourceRepository(ImmoDiggerDbContext dbContext) : IListingSourceRepository
{
    public Task<ListingSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.ListingSources.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<ListingSource?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.ListingSources.FirstOrDefaultAsync(s => s.Name == name, cancellationToken);

    public async Task<IReadOnlyList<ListingSource>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ListingSources.OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ListingSource>> GetEnabledAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ListingSources.Where(s => s.IsEnabled).ToListAsync(cancellationToken);

    public async Task AddAsync(ListingSource source, CancellationToken cancellationToken = default) =>
        await dbContext.ListingSources.AddAsync(source, cancellationToken);

    public void Update(ListingSource source) =>
        dbContext.ListingSources.Update(source);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
