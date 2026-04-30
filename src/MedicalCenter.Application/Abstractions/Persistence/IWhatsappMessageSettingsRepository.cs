using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IWhatsappMessageSettingsRepository
{
    Task<IReadOnlyCollection<WhatsappMessageSetting>> GetAllAsync(CancellationToken cancellationToken);
    Task<WhatsappMessageSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task UpsertAsync(WhatsappMessageSetting setting, CancellationToken cancellationToken);
}
