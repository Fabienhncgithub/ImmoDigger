using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ImmoDigger.Infrastructure.Persistence;

/// <summary>
/// Lets EF Core tooling (`dotnet ef migrations add`, `dotnet ef database
/// update`) build an <see cref="ImmoDiggerDbContext"/> at design time,
/// without needing the API host to start. The connection string only
/// matters for commands that actually connect to the database
/// (`database update`); `migrations add` just needs a valid Npgsql
/// provider configuration to build the model.
/// </summary>
public class ImmoDiggerDbContextFactory : IDesignTimeDbContextFactory<ImmoDiggerDbContext>
{
    public ImmoDiggerDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=immodigger;Username=immodigger;Password=immodigger";

        var optionsBuilder = new DbContextOptionsBuilder<ImmoDiggerDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ImmoDiggerDbContext(optionsBuilder.Options);
    }
}
