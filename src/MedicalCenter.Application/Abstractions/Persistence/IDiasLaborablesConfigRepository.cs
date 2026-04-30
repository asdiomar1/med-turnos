using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IDiasLaborablesConfigRepository
{
    Task<DiasLaborablesConfig?> GetAsync(string key, CancellationToken cancellationToken);
    Task UpsertAsync(DiasLaborablesConfig config, CancellationToken cancellationToken);
}
