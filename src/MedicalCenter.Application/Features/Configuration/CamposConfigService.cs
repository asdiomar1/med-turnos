using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.Configuration;

public sealed class CamposConfigService(
    ICampoConfigRepository repository,
    IAdminEventFeedRepository adminEventFeedRepository,
    IUnitOfWork unitOfWork) : ICamposConfigService
{
    private static readonly string[] AllowedTypes = ["texto", "checkbox", "numero"];

    public async Task<IReadOnlyCollection<CampoConfigSummaryDto>> GetAllAsync(CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<CampoConfigSummaryDto> CreateAsync(Guid actorUserId, string nombre, string tipo, CancellationToken cancellationToken)
    {
        var normalizedName = Normalize(nombre);
        var normalizedType = NormalizeType(tipo);
        EnsureName(normalizedName);
        await EnsureUniqueAsync(normalizedName, null, cancellationToken);

        var campoConfig = new CampoConfig(normalizedName, normalizedType, await repository.GetNextOrderAsync(cancellationToken));
        await repository.AddAsync(campoConfig, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.CampoConfigCreated,
            campoConfig.Id.ToString(),
            "Campo de configuración creado",
            $"Se creó el campo \"{campoConfig.Nombre}\" ({campoConfig.Tipo}).",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(campoConfig);
    }

    public async Task<CampoConfigSummaryDto> UpdateAsync(Guid actorUserId, Guid id, string nombre, string tipo, CancellationToken cancellationToken)
    {
        var campoConfig = await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Campo no encontrado.");
        var previousNombre = campoConfig.Nombre;
        var previousTipo = campoConfig.Tipo;
        var normalizedName = Normalize(nombre);
        var normalizedType = NormalizeType(tipo);
        EnsureName(normalizedName);
        await EnsureUniqueAsync(normalizedName, id, cancellationToken);

        campoConfig.Update(normalizedName, normalizedType);

        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.CampoConfigUpdated,
            campoConfig.Id.ToString(),
            "Campo de configuración actualizado",
            $"Se actualizó el campo \"{previousNombre}\" ({previousTipo}) → \"{campoConfig.Nombre}\" ({campoConfig.Tipo}).",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(campoConfig);
    }

    public async Task DeleteAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken)
    {
        var campoConfig = await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Campo no encontrado.");

        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.CampoConfigDeleted,
            campoConfig.Id.ToString(),
            "Campo de configuración eliminado",
            $"Se eliminó el campo \"{campoConfig.Nombre}\" ({campoConfig.Tipo}).",
            cancellationToken);

        repository.Remove(campoConfig);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByNormalizedNameAsync(normalizedName, exceptId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Ya existe un campo con ese nombre.");
        }
    }

    private static void EnsureName(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ValidationException("Nombre requerido.");
        }
    }

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string NormalizeType(string? value)
    {
        var normalized = Normalize(value).ToLowerInvariant();
        if (!AllowedTypes.Contains(normalized))
        {
            throw new ValidationException("Tipo de campo inválido.");
        }

        return normalized;
    }

    private async Task RegisterCatalogEventAsync(
        Guid actorUserId,
        string actionCode,
        string entityId,
        string title,
        string summary,
        CancellationToken cancellationToken)
    {
        var entry = new AdminEventFeedEntry(
            0,
            DateTimeOffset.UtcNow,
            actorUserId,
            AdminEventFeedConstants.DefaultActorLabel,
            actionCode,
            AdminEventFeedConstants.ActionFamilyCatalog,
            AdminEventFeedConstants.EntityTypes.CampoConfig,
            entityId,
            null,
            null,
            null,
            null,
            null,
            title,
            summary,
            AdminEventFeedConstants.SourceSystemApi,
            $"campo_config:{actionCode}:{entityId}:{Guid.NewGuid():N}",
            "{}");

        await adminEventFeedRepository.AddAsync(entry, cancellationToken);
    }

    private static CampoConfigSummaryDto Map(CampoConfig x) => new(x.Id, x.Nombre, x.Tipo, x.Orden, x.CreatedAt);
}
