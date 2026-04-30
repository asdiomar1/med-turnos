using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IWhatsappTemplateRepository
{
    Task<WhatsappTemplate?> GetActiveByKeyAsync(string key, CancellationToken cancellationToken);
    Task<WhatsappTemplate?> GetActiveByKindAsync(string kind, CancellationToken cancellationToken);
}
