using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ImmoDigger.Infrastructure.Collectors.Biddit;

/// <summary>
/// Reads a small, explicit subset of the public official PDFs attached to a
/// Biddit property. It never follows arbitrary URLs and never guesses a unit
/// count from the building type: a value is returned only when the document
/// states it explicitly.
/// </summary>
internal static partial class BidditOfficialDocumentAnalyzer
{
    private const int MaximumDocumentsToRead = 2;
    private const int MaximumDocumentBytes = 12 * 1024 * 1024;
    private const int MaximumPagesToRead = 100;

    private static readonly string[] RelevantDocumentTypes = ["URBANISM", "PROPERTY_TAX"];
    private static readonly ConcurrentDictionary<string, int> AnalysedDocumentCounts = new(StringComparer.OrdinalIgnoreCase);

    internal sealed record UnitCountEvidence(int Count, string DocumentName, string DocumentUrl);

    internal static async Task<UnitCountEvidence?> FindOfficialUnitCountAsync(
        HttpClient client,
        IReadOnlyCollection<BidditAttachmentDto> attachments,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var candidates = attachments
            .Where(attachment => RelevantDocumentTypes.Contains(attachment.Type, StringComparer.OrdinalIgnoreCase))
            .Where(attachment => IsAllowedDocumentUrl(attachment.BucketUrl))
            .OrderBy(attachment => Array.FindIndex(
                RelevantDocumentTypes,
                type => string.Equals(type, attachment.Type, StringComparison.OrdinalIgnoreCase)))
            .Take(MaximumDocumentsToRead);

        foreach (var attachment in candidates)
        {
            if (AnalysedDocumentCounts.TryGetValue(attachment.BucketUrl!, out var cachedCount))
            {
                if (cachedCount > 0)
                {
                    return new UnitCountEvidence(
                        cachedCount,
                        string.IsNullOrWhiteSpace(attachment.Name) ? "Document officiel Biddit" : attachment.Name,
                        attachment.BucketUrl!);
                }

                continue;
            }

            try
            {
                var pdfBytes = await DownloadBoundedPdfAsync(client, attachment.BucketUrl!, cancellationToken);
                if (pdfBytes is null)
                {
                    continue;
                }

                var text = ExtractPdfText(pdfBytes);
                var count = ExtractOfficialUnitCountFromText(text);
                // Biddit document URLs are immutable attachment identifiers.
                // Cache both a positive result and a conclusive "not stated"
                // result so a 15-minute collection cycle does not download the
                // same multi-megabyte PDF repeatedly.
                AnalysedDocumentCounts[attachment.BucketUrl!] = count ?? 0;
                if (count is > 0)
                {
                    return new UnitCountEvidence(
                        count.Value,
                        string.IsNullOrWhiteSpace(attachment.Name) ? "Document officiel Biddit" : attachment.Name,
                        attachment.BucketUrl!);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A missing, scanned or temporarily unavailable attachment
                // must not abort collection of the listing itself.
                logger.LogWarning(
                    exception,
                    "Biddit: official document {DocumentName} could not be analysed.",
                    attachment.Name);
            }
        }

        return null;
    }

    internal static int? ExtractOfficialUnitCountFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = WhitespaceRegex().Replace(text.Normalize(NormalizationForm.FormKC), " ");
        foreach (var pattern in UnitCountPatterns())
        {
            var match = pattern.Match(normalized);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var count) && count is > 0 and <= 200)
            {
                return count;
            }
        }

        return null;
    }

    internal static bool IsAllowedDocumentUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "www.biddit.be", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return uri.AbsolutePath.StartsWith("/stg/eco/documents/", StringComparison.OrdinalIgnoreCase) &&
               uri.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<byte[]?> DownloadBoundedPdfAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaximumDocumentBytes)
        {
            return null;
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !string.Equals(mediaType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[81_920];

        while (true)
        {
            var bytesRead = await input.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            if (output.Length + bytesRead > MaximumDocumentBytes)
            {
                return null;
            }

            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        var bytes = output.ToArray();
        return bytes.Length >= 5 && "%PDF-"u8.SequenceEqual(bytes.AsSpan(0, 5)) ? bytes : null;
    }

    private static string ExtractPdfText(byte[] pdfBytes)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(pdfBytes);

        foreach (var page in document.GetPages().Take(MaximumPagesToRead))
        {
            builder.AppendLine(ContentOrderTextExtractor.GetText(page));
        }

        return builder.ToString();
    }

    private static IReadOnlyCollection<Regex> UnitCountPatterns() =>
    [
        DutchUnitCountRegex(),
        FrenchUnitCountRegex(),
        GermanUnitCountRegex(),
    ];

    [GeneratedRegex(@"(?i)\baantal\s+(?:vergunde\s+|erkende\s+)?(?:woongelegenheden|wooneenheden|woningen|woonunits)\s*[:\-]?\s*(\d{1,3})\b")]
    private static partial Regex DutchUnitCountRegex();

    [GeneratedRegex(@"(?i)\bnombre\s+(?:officiel\s+|autorise\s+|autorisé\s+|reconnu\s+)?(?:(?:de|d['’])\s*)?(?:logements|unités?\s+(?:de\s+)?logement|unités?\s+d['’]habitation)\s*[:\-]?\s*(\d{1,3})\b")]
    private static partial Regex FrenchUnitCountRegex();

    [GeneratedRegex(@"(?i)\banzahl\s+(?:der\s+)?(?:wohneinheiten|wohnungen)\s*[:\-]?\s*(\d{1,3})\b")]
    private static partial Regex GermanUnitCountRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
