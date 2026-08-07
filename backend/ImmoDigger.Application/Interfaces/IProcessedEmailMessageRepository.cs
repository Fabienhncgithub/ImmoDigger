using ImmoDigger.Domain.Entities;

namespace ImmoDigger.Application.Interfaces;

/// <summary>Tracks which alert emails have already been imported, so the email pipeline never processes the same message twice.</summary>
public interface IProcessedEmailMessageRepository
{
    Task<bool> IsProcessedAsync(string emailMessageId, CancellationToken cancellationToken);

    Task MarkProcessedAsync(ProcessedEmailMessage record, CancellationToken cancellationToken);
}
