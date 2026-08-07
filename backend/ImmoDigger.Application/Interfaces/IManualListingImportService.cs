using ImmoDigger.Application.DTOs;

namespace ImmoDigger.Application.Interfaces;

/// <summary>
/// Imports a single listing the user points at explicitly, one URL at a
/// time - never an unattended crawl. Reads only the page's own public
/// sharing metadata (Open Graph tags, meant to be fetched by anything
/// rendering a preview of the link); never a CAPTCHA/WAF/auth bypass. When
/// that isn't enough, falls back to whatever the user pasted by hand.
/// </summary>
public interface IManualListingImportService
{
    Task<ManualImportResult> ImportFromUrlAsync(ImportUrlRequest request, CancellationToken cancellationToken);
}
