using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IWhatsappMessageActionRepository
{
    Task AddAsync(WhatsappMessageAction action, CancellationToken cancellationToken);
    Task<WhatsappMessageAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
