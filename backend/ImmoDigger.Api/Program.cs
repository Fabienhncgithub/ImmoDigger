using System.Text.Json.Serialization;
using ImmoDigger.Application;
using ImmoDigger.Infrastructure;
using ImmoDigger.Infrastructure.Persistence;
using ImmoDigger.Infrastructure.Persistence.DemoData;
using Microsoft.EntityFrameworkCore;

const string FrontendCorsPolicy = "FrontendCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    // Enums (e.g. SourceDto.CollectionMethod) serialize as their string
    // name rather than a raw ordinal - readable in responses/logs, and
    // stable if enum members are ever reordered.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Allow the local Vite dev server to call the API in development.
// Production origins will be configured via appsettings/environment
// once the frontend is deployed (Commit 10 - Docker deployment).
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Apply pending EF Core migrations, then seed reference data (the five
// V1 sources) and, if DemoMode is enabled, the fictional demo listings.
// This requires a reachable PostgreSQL instance; see .env.example /
// docker-compose.yml (added in a later commit) for local setup.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ImmoDiggerDbContext>();
    await dbContext.Database.MigrateAsync();
    await ReferenceDataSeeder.SeedAsync(dbContext);

    if (app.Configuration.GetValue<bool>("DemoMode"))
    {
        await DemoDataSeeder.SeedAsync(dbContext);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
