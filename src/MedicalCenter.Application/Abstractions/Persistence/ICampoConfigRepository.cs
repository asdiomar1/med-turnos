using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface ICampoConfigRepository
{
    Task<IReadOnlyCollection<CampoConfig>> GetAllAsync(CancellationToken cancellationToken);
    Task<CampoConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CampoConfig?> GetByNormalizedNameAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken);
    Task<int> GetNextOrderAsync(CancellationToken cancellationToken);
    Task AddAsync(CampoConfig campoConfig, CancellationToken cancellationToken);
    void Remove(CampoConfig campoConfig);
}
