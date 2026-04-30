using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Configuration;

public interface IWhatsappMessageSettingsService
{
    Task<IReadOnlyCollection<WhatsappMessageSettingDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<WhatsappMessageSettingDto> UpsertAsync(string key, string messageText, bool active, CancellationToken cancellationToken);
}
