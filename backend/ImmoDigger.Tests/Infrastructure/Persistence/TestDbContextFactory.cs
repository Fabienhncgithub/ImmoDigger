using ImmoDigger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Tests.Infrastructure.Persistence;

/// <summary>
/// Builds an <see cref="ImmoDiggerDbContext"/> backed by EF Core's
/// InMemory provider, so persistence-related tests do not require a real
/// PostgreSQL instance. Each call gets its own isolated database.
/// </summary>
internal static class TestDbContextFactory
{
    public static ImmoDiggerDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ImmoDiggerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ImmoDiggerDbContext(options);
    }
}
