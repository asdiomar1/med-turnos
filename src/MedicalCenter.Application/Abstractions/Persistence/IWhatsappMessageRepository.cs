using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IWhatsappMessageRepository
{
    Task AddAsync(WhatsappMessage message, CancellationToken cancellationToken);
    Task<WhatsappMessage?> GetByMetaMessageIdAsync(string metaMessageId, CancellationToken cancellationToken);
}
