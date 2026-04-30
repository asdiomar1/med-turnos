using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Configuration;

public interface IWorkingDaysConfigService
{
    Task<DiasLaborablesConfigDto> GetAsync(CancellationToken cancellationToken);
    Task<DiasLaborablesConfigDto> UpsertAsync(IReadOnlyCollection<short> diasSemana, CancellationToken cancellationToken);
}
