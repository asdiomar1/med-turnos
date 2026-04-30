using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Configuration;

public interface ICamposConfigService
{
    Task<IReadOnlyCollection<CampoConfigSummaryDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CampoConfigSummaryDto> CreateAsync(Guid actorUserId, string nombre, string tipo, CancellationToken cancellationToken);
    Task<CampoConfigSummaryDto> UpdateAsync(Guid actorUserId, Guid id, string nombre, string tipo, CancellationToken cancellationToken);
    Task DeleteAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken);
}
