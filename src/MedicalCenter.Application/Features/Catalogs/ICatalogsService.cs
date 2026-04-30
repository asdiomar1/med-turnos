using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Catalogs;

public interface ICatalogsService
{
    Task<IReadOnlyCollection<CondicionIvaSummaryDto>> GetCondicionesIvaAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ObraSocialSummaryDto>> GetObrasSocialesAsync(CancellationToken cancellationToken);
    Task<ObraSocialSummaryDto> CreateObraSocialAsync(Guid actorUserId, string nombre, bool tieneConvenio, string? abreviatura, CancellationToken cancellationToken);
    Task<ObraSocialSummaryDto> UpdateObraSocialAsync(Guid actorUserId, int obraSocialId, string nombre, bool tieneConvenio, string? abreviatura, CancellationToken cancellationToken);
    Task<ObraSocialSummaryDto> SetObraSocialActiveAsync(Guid actorUserId, int obraSocialId, bool activa, CancellationToken cancellationToken);
    Task<ObraSocialSummaryDto> SetObraSocialConvenioAsync(Guid actorUserId, int obraSocialId, bool tieneConvenio, CancellationToken cancellationToken);
}
