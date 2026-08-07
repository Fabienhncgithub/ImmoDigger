using ImmoDigger.Application.Interfaces;
using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoDigger.Infrastructure.Persistence.Repositories;

public class ProcessedEmailMessageRepository(ImmoDiggerDbContext dbContext) : IProcessedEmailMessageRepository
{
    public Task<bool> IsProcessedAsync(string emailMessageId, CancellationToken cancellationToken) =>
        dbContext.ProcessedEmailMessages.AnyAsync(m => m.EmailMessageId == emailMessageId, cancellationToken);

    public async Task MarkProcessedAsync(ProcessedEmailMessage record, CancellationToken cancellationToken)
    {
        await dbContext.ProcessedEmailMessages.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
