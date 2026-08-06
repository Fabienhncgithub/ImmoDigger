using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence.Repositories;

public class SearchProfileRepository(ImmoDiggerDbContext dbContext) : ISearchProfileRepository
{
    public Task<SearchProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.SearchProfiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SearchProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SearchProfiles.OrderBy(p => p.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SearchProfile>> GetEnabledAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SearchProfiles.Where(p => p.IsEnabled).ToListAsync(cancellationToken);

    public async Task AddAsync(SearchProfile profile, CancellationToken cancellationToken = default) =>
        await dbContext.SearchProfiles.AddAsync(profile, cancellationToken);

    public void Update(SearchProfile profile) =>
        dbContext.SearchProfiles.Update(profile);

    public void Remove(SearchProfile profile) =>
        dbContext.SearchProfiles.Remove(profile);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
