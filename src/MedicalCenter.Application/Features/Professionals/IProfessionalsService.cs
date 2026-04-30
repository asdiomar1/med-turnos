using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Professionals;

public interface IProfessionalsService
{
    Task<IReadOnlyCollection<MedicoSummaryDto>> GetMedicosAsync(CancellationToken cancellationToken);
    Task<MedicoSummaryDto> CreateMedicoAsync(Guid actorUserId, string nombre, CancellationToken cancellationToken);
    Task<MedicoSummaryDto> UpdateMedicoAsync(Guid actorUserId, int medicoId, string nombre, CancellationToken cancellationToken);
    Task<MedicoSummaryDto> SetMedicoActiveAsync(Guid actorUserId, int medicoId, bool activo, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ReferenteSummaryDto>> GetReferentesAsync(CancellationToken cancellationToken);
    Task<ReferenteSummaryDto> CreateReferenteAsync(Guid actorUserId, string nombre, string tipo, CancellationToken cancellationToken);
    Task<ReferenteSummaryDto> UpdateReferenteAsync(Guid actorUserId, int referenteId, string nombre, string tipo, CancellationToken cancellationToken);
    Task<ReferenteSummaryDto> SetReferenteActiveAsync(Guid actorUserId, int referenteId, bool activo, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<OperadorCamaraSummaryDto>> GetOperadoresAsync(CancellationToken cancellationToken);
}
