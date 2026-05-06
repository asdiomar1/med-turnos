using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.Catalogs;

public sealed class CatalogsService(
    ICondicionIvaRepository condicionIvaRepository,
    IObraSocialRepository obraSocialRepository,
    IAdminEventFeedRepository adminEventFeedRepository,
    IUnitOfWork unitOfWork) : ICatalogsService
{
    private const string ObraSocialEntityType = "obra_social";
    public async Task<IReadOnlyCollection<CondicionIvaSummaryDto>> GetCondicionesIvaAsync(bool includeInactive, CancellationToken cancellationToken) =>
        (await condicionIvaRepository.GetAllAsync(includeInactive, cancellationToken)).Select(x => x.ToSummary()).ToArray();

    public async Task<CondicionIvaSummaryDto> CreateCondicionIvaAsync(Guid actorUserId, string nombre, CancellationToken cancellationToken)
    {
        var normalizedName = Normalize(nombre);
        EnsureName(normalizedName);
        await EnsureUniqueCondicionIvaNameAsync(normalizedName, null, cancellationToken);

        var condicionIva = new CondicionIva(0, normalizedName, true, await condicionIvaRepository.GetNextOrderAsync(cancellationToken));
        await condicionIvaRepository.AddAsync(condicionIva, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RegisterCatalogEventAsync(new CatalogEventParams(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.CondicionIvaCreated,
            condicionIva.Id.ToString(),
            AdminEventFeedConstants.EntityTypes.CondicionIva,
            "condicion_iva",
            "Condición IVA creada",
            $"Se creó la condición IVA \"{condicionIva.Nombre}\"."),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return condicionIva.ToSummary();
    }

    public async Task<CondicionIvaSummaryDto> UpdateCondicionIvaAsync(Guid actorUserId, int condicionIvaId, string nombre, int orden, CancellationToken cancellationToken)
    {
        var condicionIva = await condicionIvaRepository.GetByIdAsync(condicionIvaId, cancellationToken) ?? throw new NotFoundException("Condición IVA no encontrada.");
        var previousNombre = condicionIva.Nombre;
        var normalizedName = Normalize(nombre);
        EnsureName(normalizedName);
        await EnsureUniqueCondicionIvaNameAsync(normalizedName, condicionIvaId, cancellationToken);
        condicionIva.Update(normalizedName, orden);
        await RegisterCatalogEventAsync(new CatalogEventParams(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.CondicionIvaUpdated,
            condicionIva.Id.ToString(),
            AdminEventFeedConstants.EntityTypes.CondicionIva,
            "condicion_iva",
            "Condición IVA actualizada",
            $"Se actualizó la condición IVA \"{previousNombre}\" → \"{condicionIva.Nombre}\"."),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await condicionIvaRepository.InvalidateCacheAsync(cancellationToken);
        return condicionIva.ToSummary();
    }

    public async Task<CondicionIvaSummaryDto> SetCondicionIvaActiveAsync(Guid actorUserId, int condicionIvaId, bool activo, CancellationToken cancellationToken)
    {
        var condicionIva = await condicionIvaRepository.GetByIdAsync(condicionIvaId, cancellationToken) ?? throw new NotFoundException("Condición IVA no encontrada.");
        condicionIva.SetActive(activo);
        await RegisterCatalogEventAsync(new CatalogEventParams(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.CondicionIvaStatusUpdated,
            condicionIva.Id.ToString(),
            AdminEventFeedConstants.EntityTypes.CondicionIva,
            "condicion_iva",
            "Estado de condición IVA actualizado",
            $"La condición IVA \"{condicionIva.Nombre}\" quedó {(condicionIva.Activo ? "activa" : "inactiva")}."),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await condicionIvaRepository.InvalidateCacheAsync(cancellationToken);
        return condicionIva.ToSummary();
    }

    public async Task<IReadOnlyCollection<ObraSocialSummaryDto>> GetObrasSocialesAsync(CancellationToken cancellationToken) =>
        (await obraSocialRepository.GetAllAsync(cancellationToken)).Select(x => x.ToSummary()).ToArray();

    public async Task<ObraSocialSummaryDto> CreateObraSocialAsync(Guid actorUserId, string nombre, bool tieneConvenio, string? abreviatura, CancellationToken cancellationToken)
    {
        var normalizedName = Normalize(nombre);
        EnsureName(normalizedName);
        await EnsureUniqueNameAsync(normalizedName, null, cancellationToken);

        var obraSocial = new ObraSocial(0, normalizedName, true, tieneConvenio, 0, NormalizeAbreviatura(abreviatura));
        await obraSocialRepository.AddAsync(obraSocial, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await RegisterCatalogEventAsync(new CatalogEventParams(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ObraSocialCreated,
            obraSocial.Id.ToString(),
            AdminEventFeedConstants.EntityTypes.ObraSocial,
            ObraSocialEntityType,
            "Obra social creada",
            $"Se creó la obra social \"{obraSocial.Nombre}\"."),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return obraSocial.ToSummary();
    }

    public async Task<ObraSocialSummaryDto> UpdateObraSocialAsync(Guid actorUserId, int obraSocialId, string nombre, bool tieneConvenio, string? abreviatura, CancellationToken cancellationToken)
    {
        var obraSocial = await obraSocialRepository.GetByIdAsync(obraSocialId, cancellationToken) ?? throw new NotFoundException("Obra social no encontrada.");
        var previousNombre = obraSocial.Nombre;
        var normalizedName = Normalize(nombre);
        EnsureName(normalizedName);
        await EnsureUniqueNameAsync(normalizedName, obraSocialId, cancellationToken);
        obraSocial.Update(normalizedName, tieneConvenio, NormalizeAbreviatura(abreviatura));
        await RegisterCatalogEventAsync(new CatalogEventParams(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ObraSocialUpdated,
            obraSocial.Id.ToString(),
            AdminEventFeedConstants.EntityTypes.ObraSocial,
            ObraSocialEntityType,
            "Obra social actualizada",
            $"Se actualizó la obra social \"{previousNombre}\" → \"{obraSocial.Nombre}\"."),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return obraSocial.ToSummary();
    }

    public async Task<ObraSocialSummaryDto> SetObraSocialActiveAsync(Guid actorUserId, int obraSocialId, bool activa, CancellationToken cancellationToken)
    {
        var obraSocial = await obraSocialRepository.GetByIdAsync(obraSocialId, cancellationToken) ?? throw new NotFoundException("Obra social no encontrada.");
        obraSocial.SetActive(activa);
        await RegisterCatalogEventAsync(new CatalogEventParams(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ObraSocialStatusUpdated,
            obraSocial.Id.ToString(),
            AdminEventFeedConstants.EntityTypes.ObraSocial,
            ObraSocialEntityType,
            "Estado de obra social actualizado",
            $"La obra social \"{obraSocial.Nombre}\" quedó {(obraSocial.Activa ? "activa" : "inactiva")}."),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return obraSocial.ToSummary();
    }

    public async Task<ObraSocialSummaryDto> SetObraSocialConvenioAsync(Guid actorUserId, int obraSocialId, bool tieneConvenio, CancellationToken cancellationToken)
    {
        var obraSocial = await obraSocialRepository.GetByIdAsync(obraSocialId, cancellationToken) ?? throw new NotFoundException("Obra social no encontrada.");
        obraSocial.SetTieneConvenio(tieneConvenio);
        await RegisterCatalogEventAsync(new CatalogEventParams(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ObraSocialConvenioUpdated,
            obraSocial.Id.ToString(),
            AdminEventFeedConstants.EntityTypes.ObraSocial,
            ObraSocialEntityType,
            "Convenio de obra social actualizado",
            $"La obra social \"{obraSocial.Nombre}\" quedó con convenio={(obraSocial.TieneConvenio ? "sí" : "no")}."),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return obraSocial.ToSummary();
    }

    private async Task EnsureUniqueNameAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken)
    {
        var exists = await obraSocialRepository.GetByNormalizedNameAsync(normalizedName, exceptId, cancellationToken);
        if (exists is not null)
        {
            throw new ConflictException("Ya existe una obra social con ese nombre.");
        }
    }

    private async Task EnsureUniqueCondicionIvaNameAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken)
    {
        var exists = await condicionIvaRepository.GetByNormalizedNameAsync(normalizedName, exceptId, cancellationToken);
        if (exists is not null)
        {
            throw new ConflictException("Ya existe una condición IVA con ese nombre.");
        }
    }

    private static void EnsureName(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ValidationException("El nombre es requerido.");
        }
    }

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    private static string? NormalizeAbreviatura(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private sealed record CatalogEventParams(
        Guid ActorUserId,
        string ActionCode,
        string EntityId,
        string EntityType,
        string SourcePrefix,
        string Title,
        string Summary);

    private async Task RegisterCatalogEventAsync(CatalogEventParams p, CancellationToken cancellationToken)
    {
        var entry = new AdminEventFeedEntry(new AdminEventFeedEntryCreateParams(
            0,
            DateTimeOffset.UtcNow,
            p.ActorUserId,
            AdminEventFeedConstants.DefaultActorLabel,
            p.ActionCode,
            AdminEventFeedConstants.ActionFamilyCatalog,
            p.EntityType,
            p.EntityId,
            null,
            null,
            null,
            null,
            null,
            p.Title,
            p.Summary,
            AdminEventFeedConstants.SourceSystemApi,
            $"{p.SourcePrefix}:{p.ActionCode}:{p.EntityId}:{Guid.NewGuid():N}",
            "{}"));

        await adminEventFeedRepository.AddAsync(entry, cancellationToken);
    }
}
