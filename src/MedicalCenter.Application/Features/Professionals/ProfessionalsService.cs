using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.Professionals;

public sealed class ProfessionalsService(
    IReferenteRepository referenteRepository,
    IUserRepository userRepository,
    IAdminEventFeedRepository adminEventFeedRepository,
    IUnitOfWork unitOfWork) : IProfessionalsService
{
    private const string AgenciaEntityType = "agencia";
    public async Task<IReadOnlyCollection<MedicoSummaryDto>> GetMedicosAsync(CancellationToken cancellationToken) =>
        (await userRepository.GetByRoleAsync("medico", onlyActive: true, cancellationToken))
            .Select(u => u.ToMedicoSummary())
            .ToArray();

    public async Task<IReadOnlyCollection<ReferenteSummaryDto>> GetReferentesAsync(CancellationToken cancellationToken) =>
        (await referenteRepository.GetAsync(cancellationToken)).Select(r => r.ToSummary()).ToArray();

    public async Task<ReferenteSummaryDto> CreateReferenteAsync(Guid actorUserId, string nombre, string tipo, CancellationToken cancellationToken)
    {
        var normalizedName = Normalize(nombre);
        var normalizedType = NormalizeReferenteType(tipo);
        EnsureName(normalizedName);
        await EnsureReferenteUniqueAsync(normalizedName, normalizedType, null, cancellationToken);

        var referente = new Referente(normalizedName, normalizedType, await referenteRepository.GetNextOrderAsync(cancellationToken));
        await referenteRepository.AddAsync(referente, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ReferenteCreated,
            AdminEventFeedConstants.EntityTypes.Referente,
            referente.Id.ToString(),
            "Referente creado",
            $"Se creó el referente \"{referente.Nombre}\" ({NormalizeReferenteTypeForResponse(referente.Tipo)}).",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return referente.ToSummary();
    }

    public async Task<ReferenteSummaryDto> UpdateReferenteAsync(Guid actorUserId, int referenteId, string nombre, string tipo, CancellationToken cancellationToken)
    {
        var referente = await referenteRepository.GetByIdAsync(referenteId, cancellationToken) ?? throw new NotFoundException("Referente no encontrado.");
        var previousNombre = referente.Nombre;
        var previousTipo = NormalizeReferenteTypeForResponse(referente.Tipo);
        var normalizedName = Normalize(nombre);
        var normalizedType = NormalizeReferenteType(tipo);
        EnsureName(normalizedName);
        await EnsureReferenteUniqueAsync(normalizedName, normalizedType, referenteId, cancellationToken);

        referente.Update(normalizedName, normalizedType);

        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ReferenteUpdated,
            AdminEventFeedConstants.EntityTypes.Referente,
            referente.Id.ToString(),
            "Referente actualizado",
            $"Se actualizó el referente \"{previousNombre}\" ({previousTipo}) → \"{referente.Nombre}\" ({NormalizeReferenteTypeForResponse(referente.Tipo)}).",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return referente.ToSummary();
    }

    public async Task<ReferenteSummaryDto> SetReferenteActiveAsync(Guid actorUserId, int referenteId, bool activo, CancellationToken cancellationToken)
    {
        var referente = await referenteRepository.GetByIdAsync(referenteId, cancellationToken) ?? throw new NotFoundException("Referente no encontrado.");
        referente.SetActive(activo);

        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ReferenteStatusUpdated,
            AdminEventFeedConstants.EntityTypes.Referente,
            referente.Id.ToString(),
            "Estado de referente actualizado",
            $"El referente \"{referente.Nombre}\" quedó {(referente.Activo ? "activo" : "inactivo")}.",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return referente.ToSummary();
    }

    public async Task<IReadOnlyCollection<OperadorCamaraSummaryDto>> GetOperadoresAsync(CancellationToken cancellationToken) =>
        (await userRepository.GetStaffAsync(false, cancellationToken))
            .Where(x => x.IsActive)
            .Select(x => new OperadorCamaraSummaryDto(x.Id, x.Nombre ?? x.Identifier, x.IsActive))
            .ToArray();

    private async Task EnsureReferenteUniqueAsync(string normalizedName, string normalizedType, int? exceptId, CancellationToken cancellationToken)
    {
        var existing = await referenteRepository.GetByNormalizedNameAndTypeAsync(normalizedName, normalizedType, exceptId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Ya existe un referente con ese nombre y tipo.");
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

    private static string NormalizeReferenteType(string? tipo)
    {
        var normalized = Normalize(tipo).ToLowerInvariant();
        return normalized switch
        {
            "doctor" => "doctor",
            AgenciaEntityType => AgenciaEntityType,
            "institucion" => AgenciaEntityType,
            "otro" => "otro",
            _ => throw new ValidationException("Tipo de referente inválido.")
        };
    }

    private static string NormalizeReferenteTypeForResponse(string tipo) =>
        tipo == AgenciaEntityType ? "institucion" : tipo;

    private async Task RegisterCatalogEventAsync(
        Guid actorUserId,
        string actionCode,
        string entityType,
        string entityId,
        string title,
        string summary,
        CancellationToken cancellationToken)
    {
        var entry = new AdminEventFeedEntry(new AdminEventFeedEntryCreateParams(
            0,
            DateTimeOffset.UtcNow,
            actorUserId,
            AdminEventFeedConstants.DefaultActorLabel,
            actionCode,
            AdminEventFeedConstants.ActionFamilyCatalog,
            entityType,
            entityId,
            null,
            null,
            null,
            null,
            null,
            title,
            summary,
            AdminEventFeedConstants.SourceSystemApi,
            $"{entityType}:{actionCode}:{entityId}:{Guid.NewGuid():N}",
            "{}"));

        await adminEventFeedRepository.AddAsync(entry, cancellationToken);
    }
}
