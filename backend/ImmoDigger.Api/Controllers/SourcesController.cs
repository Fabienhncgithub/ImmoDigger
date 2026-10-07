using ImmoDigger.Application.DTOs;
using ImmoDigger.Application.Interfaces;
using ImmoDigger.Application.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace ImmoDigger.Api.Controllers;

[ApiController]
[Route("api/sources")]
public class SourcesController(
    IListingSourceRepository sourceRepository,
    IPropertyListingRepository listingRepository,
    IEnumerable<IEmailListingParser> emailParsers,
    IEmailInbox emailInbox,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SourceDto>>> GetAll(CancellationToken cancellationToken)
    {
        var sources = await sourceRepository.GetAllAsync(cancellationToken);
        var dtos = new List<SourceDto>();

        foreach (var source in sources)
        {
            var count = await listingRepository.CountBySourceAsync(source.Name, cancellationToken);
            dtos.Add(source.ToDto(count));
        }

        return Ok(dtos);
    }

    /// <summary>
    /// Reports whether the IMAP alert pipeline is ready, without exposing
    /// host credentials or mailbox contents.
    /// </summary>
    [HttpGet("email-import/status")]
    public ActionResult<EmailImportStatusDto> GetEmailImportStatus()
    {
        var host = configuration["Imap:Host"];
        var username = configuration["Imap:Username"];
        var password = configuration["Imap:Password"];
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(host)) issues.Add("IMAP_HOST manquant");
        if (string.IsNullOrWhiteSpace(username)) issues.Add("IMAP_USERNAME manquant");
        if (string.IsNullOrWhiteSpace(password)) issues.Add("IMAP_PASSWORD manquant");

        var folder = configuration["Imap:Folder"];
        var lookbackDays = configuration.GetValue<int?>("Imap:LookbackDays") ?? 7;
        var autoEnable = configuration.GetValue<bool?>("Imap:AutoEnable") ?? true;

        return Ok(new EmailImportStatusDto(
            IsConfigured: issues.Count == 0,
            AutoEnable: autoEnable,
            Folder: string.IsNullOrWhiteSpace(folder) ? "INBOX" : folder,
            LookbackDays: Math.Clamp(lookbackDays, 1, 90),
            SupportedSources: emailParsers.Select(parser => parser.SourceName).Distinct().Order().ToList(),
            ConfigurationIssues: issues));
    }

    /// <summary>
    /// Verifies the configured IMAP connection and reports only how many
    /// messages are visible in the lookback window. No message content or
    /// credential is returned, and no IMAP flag is changed.
    /// </summary>
    [HttpPost("email-import/test")]
    public async Task<ActionResult<EmailImportConnectionTestDto>> TestEmailImport(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration["Imap:Host"]) ||
            string.IsNullOrWhiteSpace(configuration["Imap:Username"]) ||
            string.IsNullOrWhiteSpace(configuration["Imap:Password"]))
        {
            return BadRequest(new EmailImportConnectionTestDto(
                false, 0, "Configuration IMAP incomplète."));
        }

        try
        {
            var messages = await emailInbox.FetchNewMessagesAsync(cancellationToken);
            return Ok(new EmailImportConnectionTestDto(
                true,
                messages.Count,
                $"Connexion IMAP réussie : {messages.Count} message(s) visible(s) dans la fenêtre configurée."));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new EmailImportConnectionTestDto(
                    false, 0, "Connexion IMAP impossible. Vérifiez l'hôte, le port, TLS et le mot de passe d'application."));
        }
    }

    /// <summary>Toggles activation and/or updates the polling cadence.</summary>
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<SourceDto>> Update(
        Guid id, [FromBody] UpdateSourceRequest request, CancellationToken cancellationToken)
    {
        var source = await sourceRepository.GetByIdAsync(id, cancellationToken);
        if (source is null)
        {
            return NotFound();
        }

        source.IsEnabled = request.IsEnabled;
        if (request.PollingIntervalMinutes is > 0)
        {
            source.PollingIntervalMinutes = request.PollingIntervalMinutes.Value;
        }

        sourceRepository.Update(source);
        await sourceRepository.SaveChangesAsync(cancellationToken);

        var count = await listingRepository.CountBySourceAsync(source.Name, cancellationToken);
        return Ok(source.ToDto(count));
    }
}
