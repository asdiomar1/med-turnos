using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class WhatsappDispatchQueueRepository(MedicalCenterDbContext dbContext) : IWhatsappDispatchQueueRepository
{
    public async Task<IReadOnlyCollection<WhatsappDispatchQueueItem>> ClaimAsync(int limit, IReadOnlyCollection<Guid> slotIds, CancellationToken cancellationToken)
    {
        var normalizedLimit = Math.Max(limit, 1);
        var ids = slotIds.Where(x => x != Guid.Empty).Distinct().ToArray();

        IQueryable<WhatsappDispatchQueueItem> query = dbContext.WhatsappDispatchQueueItems
            .Where(x => x.Status == "pending" || x.Status == "failed")
            .OrderBy(x => x.CreatedAt);

        if (ids.Length > 0)
        {
            query = query.Where(x => x.SlotId.HasValue && ids.Contains(x.SlotId.Value));
        }

        var items = await query.Take(normalizedLimit).ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var item in items)
        {
            item.MarkProcessing(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return items;
    }

    public async Task<bool> TryEnqueueAsync(WhatsappDispatchQueueItem item, CancellationToken cancellationToken)
    {
        var trackedDuplicate = dbContext.ChangeTracker
            .Entries<WhatsappDispatchQueueItem>()
            .Any(x => string.Equals(x.Entity.IdempotencyKey, item.IdempotencyKey, StringComparison.Ordinal));

        if (trackedDuplicate)
        {
            return false;
        }

        var existing = await dbContext.WhatsappDispatchQueueItems.AnyAsync(x => x.IdempotencyKey == item.IdempotencyKey, cancellationToken);
        if (existing)
        {
            return false;
        }

        await dbContext.WhatsappDispatchQueueItems.AddAsync(item, cancellationToken);
        return true;
    }
}
