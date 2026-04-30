using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IWhatsappDispatchQueueRepository
{
    Task<IReadOnlyCollection<WhatsappDispatchQueueItem>> ClaimAsync(int limit, IReadOnlyCollection<Guid> slotIds, CancellationToken cancellationToken);
    Task<bool> TryEnqueueAsync(WhatsappDispatchQueueItem item, CancellationToken cancellationToken);
}
