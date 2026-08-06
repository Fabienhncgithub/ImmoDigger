using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Interfaces;

public interface ISearchProfileRepository
{
    Task<SearchProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SearchProfile>> GetEnabledAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SearchProfile profile, CancellationToken cancellationToken = default);

    void Update(SearchProfile profile);

    void Remove(SearchProfile profile);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
