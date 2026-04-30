using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.Professionals;

public sealed class ProfessionalsService(
    IMedicoRepository medicoRepository,
    IReferenteRepository referenteRepository,
    IUserRepository userRepository,
    IAdminEventFeedRepository adminEventFeedRepository,
    IUnitOfWork unitOfWork) : IProfessionalsService
{
    public async Task<IReadOnlyCollection<MedicoSummaryDto>> GetMedicosAsync(CancellationToken cancellationToken) =>
        (await medicoRepository.GetAsync(false, cancellationToken)).Select(Map).ToArray();

    public async Task<MedicoSummaryDto> CreateMedicoAsync(Guid actorUserId, string nombre, CancellationToken cancellationToken)
    {
        var normalizedName = Normalize(nombre);
        EnsureName(normalizedName);
        await EnsureMedicoUniqueAsync(normalizedName, null, cancellationToken);

        var medico = new Medico(normalizedName, await medicoRepository.GetNextOrderAsync(cancellationToken));
        await medicoRepository.AddAsync(medico, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.MedicoCreated,
            AdminEventFeedConstants.EntityTypes.Medico,
            medico.Id.ToString(),
            "Médico creado",
            $"Se creó el médico \"{medico.Nombre}\".",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(medico);
    }

    public async Task<MedicoSummaryDto> UpdateMedicoAsync(Guid actorUserId, int medicoId, string nombre, CancellationToken cancellationToken)
    {
        var medico = await medicoRepository.GetByIdAsync(medicoId, cancellationToken) ?? throw new NotFoundException("Médico no encontrado.");
        var previousNombre = medico.Nombre;
        var normalizedName = Normalize(nombre);
        EnsureName(normalizedName);
        await EnsureMedicoUniqueAsync(normalizedName, medicoId, cancellationToken);

        medico.UpdateNombre(normalizedName);

        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.MedicoUpdated,
            AdminEventFeedConstants.EntityTypes.Medico,
            medico.Id.ToString(),
            "Médico actualizado",
            $"Se actualizó el médico \"{previousNombre}\" → \"{medico.Nombre}\".",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(medico);
    }

    public async Task<MedicoSummaryDto> SetMedicoActiveAsync(Guid actorUserId, int medicoId, bool activo, CancellationToken cancellationToken)
    {
        var medico = await medicoRepository.GetByIdAsync(medicoId, cancellationToken) ?? throw new NotFoundException("Médico no encontrado.");
        medico.SetActive(activo);

        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.MedicoStatusUpdated,
            AdminEventFeedConstants.EntityTypes.Medico,
            medico.Id.ToString(),
            "Estado de médico actualizado",
            $"El médico \"{medico.Nombre}\" quedó {(medico.Activo ? "activo" : "inactivo")}.",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(medico);
    }

    public async Task<IReadOnlyCollection<ReferenteSummaryDto>> GetReferentesAsync(CancellationToken cancellationToken) =>
        (await referenteRepository.GetAsync(cancellationToken)).Select(Map).ToArray();

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
        return Map(referente);
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
        return Map(referente);
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
        return Map(referente);
    }

    public async Task<IReadOnlyCollection<OperadorCamaraSummaryDto>> GetOperadoresAsync(CancellationToken cancellationToken) =>
        (await userRepository.GetStaffAsync(false, cancellationToken))
            .Where(x => x.IsActive)
            .Select(x => new OperadorCamaraSummaryDto(x.Id, x.Nombre ?? x.Identifier, x.IsActive))
            .ToArray();

    private async Task EnsureMedicoUniqueAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken)
    {
        var existing = await medicoRepository.GetByNormalizedNameAsync(normalizedName, exceptId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Ya existe un médico con ese nombre.");
        }
    }

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
            "agencia" => "agencia",
            "institucion" => "agencia",
            "otro" => "otro",
            _ => throw new ValidationException("Tipo de referente inválido.")
        };
    }

    private static string NormalizeReferenteTypeForResponse(string tipo) =>
        tipo == "agencia" ? "institucion" : tipo;

    private async Task RegisterCatalogEventAsync(
        Guid actorUserId,
        string actionCode,
        string entityType,
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
            "{}");

        await adminEventFeedRepository.AddAsync(entry, cancellationToken);
    }

    private static MedicoSummaryDto Map(Medico medico) =>
        new(medico.Id, medico.Nombre, medico.Activo, medico.Orden, medico.CreatedAt, medico.PerfilId);

    private static ReferenteSummaryDto Map(Referente referente) =>
        new(referente.Id, referente.Nombre, NormalizeReferenteTypeForResponse(referente.Tipo), referente.Activo, referente.Orden, referente.CreatedAt, referente.UpdatedAt);
}
