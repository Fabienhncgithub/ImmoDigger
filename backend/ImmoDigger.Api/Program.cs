using System.Text.Json.Serialization;
using ImmoDigger.Application;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Infrastructure;
using ImmoDigger.Infrastructure.Persistence;
using ImmoDigger.Infrastructure.Persistence.DemoData;
using ImmoDigger.Infrastructure.EmailImport;
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

// Allow configured frontends to call the API. The Docker deployment uses
// a same-origin reverse proxy, while local Vite uses localhost:5173.
var frontendOrigins = builder.Configuration
    .GetSection("Frontend:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(frontendOrigins)
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

    // Fresh production installations should not silently sit with a fully
    // configured mailbox and a disabled EmailImport source. AutoEnable is an
    // explicit IMAP setting and can be turned off when manual control is
    // preferred.
    var imap = app.Configuration.GetSection(ImapSettings.SectionName).Get<ImapSettings>() ?? new ImapSettings();
    var imapConfigured = !string.IsNullOrWhiteSpace(imap.Host) &&
                         !string.IsNullOrWhiteSpace(imap.Username) &&
                         !string.IsNullOrWhiteSpace(imap.Password);
    if (imapConfigured && imap.AutoEnable)
    {
        var emailImport = await dbContext.ListingSources.SingleAsync(source => source.Name == "EmailImport");
        if (!emailImport.IsEnabled)
        {
            emailImport.IsEnabled = true;
            await dbContext.SaveChangesAsync();
        }
    }

    if (app.Configuration.GetValue<bool>("DemoMode"))
    {
        await DemoDataSeeder.SeedAsync(dbContext);
    }

    var removedDuplicates = await DuplicateListingCleanup.RunAsync(dbContext);
    if (removedDuplicates > 0)
    {
        app.Logger.LogInformation("Removed {Count} duplicate listing(s) at startup.", removedDuplicates);
    }

    await UrbanisticStatusBackfill.RunAsync(
        dbContext, scope.ServiceProvider.GetRequiredService<IInvestmentAnalysisService>());
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// TLS is normally terminated by the deployment reverse proxy. Enable this
// explicitly only when Kestrel itself also has an HTTPS endpoint configured.
if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

// Lightweight readiness endpoint used by Docker and pre-production smoke
// checks. It verifies the API process and its PostgreSQL dependency.
app.MapGet("/health", async (ImmoDiggerDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return Results.Json(
            new { status = canConnect ? "healthy" : "unhealthy" },
            statusCode: canConnect ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(new { status = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).ExcludeFromDescription();

app.Run();
