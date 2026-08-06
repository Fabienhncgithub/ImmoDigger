using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Interfaces;

public interface IListingSourceRepository
{
    Task<ListingSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ListingSource?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListingSource>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListingSource>> GetEnabledAsync(CancellationToken cancellationToken = default);

    Task AddAsync(ListingSource source, CancellationToken cancellationToken = default);

    void Update(ListingSource source);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
