using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.Configuration;

public sealed class WhatsappMessageSettingsService(IWhatsappMessageSettingsRepository repository) : IWhatsappMessageSettingsService
{
    public async Task<IReadOnlyCollection<WhatsappMessageSettingDto>> GetAllAsync(CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).Select(s => s.ToDto()).ToArray();

    public async Task<WhatsappMessageSettingDto> UpsertAsync(string key, string messageText, bool active, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeKey(key);
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            throw new ValidationException("Clave requerida.");
        }

        if (active && string.IsNullOrWhiteSpace(messageText))
        {
            throw new ValidationException("El mensaje no puede estar vacio si la configuracion esta activa.");
        }

        if (!string.IsNullOrWhiteSpace(messageText) && messageText.Length > 500)
        {
            throw new ValidationException("El mensaje no puede superar los 500 caracteres.");
        }

        var existing = await repository.GetByKeyAsync(normalizedKey, cancellationToken);
        var normalizedMessageText = messageText?.Trim() ?? string.Empty;
        var setting = existing is null
            ? new WhatsappMessageSetting(normalizedKey, HumanizeKey(normalizedKey), null, normalizedMessageText, active)
            : existing;

        if (existing is not null)
        {
            existing.Update(normalizedMessageText, active);
        }
        else
        {
            await repository.UpsertAsync(setting, cancellationToken);
        }

        if (existing is null)
        {
            return setting.ToDto();
        }

        await repository.UpsertAsync(existing, cancellationToken);
        return existing.ToDto();
    }

    private static string NormalizeKey(string? key) => string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
    private static string HumanizeKey(string key) => key.Replace('_', ' ');
}
