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
    public async Task<IReadOnlyCollection<CondicionIvaSummaryDto>> GetCondicionesIvaAsync(bool includeInactive, CancellationToken cancellationToken) =>
        (await condicionIvaRepository.GetAllAsync(includeInactive, cancellationToken)).Select(x => x.ToSummary()).ToArray();

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
        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ObraSocialCreated,
            obraSocial.Id.ToString(),
            "Obra social creada",
            $"Se creó la obra social \"{obraSocial.Nombre}\".",
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
        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ObraSocialUpdated,
            obraSocial.Id.ToString(),
            "Obra social actualizada",
            $"Se actualizó la obra social \"{previousNombre}\" → \"{obraSocial.Nombre}\".",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return obraSocial.ToSummary();
    }

    public async Task<ObraSocialSummaryDto> SetObraSocialActiveAsync(Guid actorUserId, int obraSocialId, bool activa, CancellationToken cancellationToken)
    {
        var obraSocial = await obraSocialRepository.GetByIdAsync(obraSocialId, cancellationToken) ?? throw new NotFoundException("Obra social no encontrada.");
        obraSocial.SetActive(activa);
        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ObraSocialStatusUpdated,
            obraSocial.Id.ToString(),
            "Estado de obra social actualizado",
            $"La obra social \"{obraSocial.Nombre}\" quedó {(obraSocial.Activa ? "activa" : "inactiva")}.",
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return obraSocial.ToSummary();
    }

    public async Task<ObraSocialSummaryDto> SetObraSocialConvenioAsync(Guid actorUserId, int obraSocialId, bool tieneConvenio, CancellationToken cancellationToken)
    {
        var obraSocial = await obraSocialRepository.GetByIdAsync(obraSocialId, cancellationToken) ?? throw new NotFoundException("Obra social no encontrada.");
        obraSocial.SetTieneConvenio(tieneConvenio);
        await RegisterCatalogEventAsync(
            actorUserId,
            AdminEventFeedConstants.ActionCodes.ObraSocialConvenioUpdated,
            obraSocial.Id.ToString(),
            "Convenio de obra social actualizado",
            $"La obra social \"{obraSocial.Nombre}\" quedó con convenio={(obraSocial.TieneConvenio ? "sí" : "no")}.",
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

    private static void EnsureName(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ValidationException("Nombre requerido.");
        }
    }

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    private static string? NormalizeAbreviatura(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

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
            AdminEventFeedConstants.EntityTypes.ObraSocial,
            entityId,
            null,
            null,
            null,
            null,
            null,
            title,
            summary,
            AdminEventFeedConstants.SourceSystemApi,
            $"obra_social:{actionCode}:{entityId}:{Guid.NewGuid():N}",
            "{}");

        await adminEventFeedRepository.AddAsync(entry, cancellationToken);
    }
}
