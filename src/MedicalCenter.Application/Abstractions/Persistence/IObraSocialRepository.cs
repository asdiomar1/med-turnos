using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IObraSocialRepository
{
    Task<IReadOnlyCollection<ObraSocial>> GetAllAsync(CancellationToken cancellationToken);
    Task<ObraSocial?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ObraSocial?> GetByNormalizedNameAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ObraSocial>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken);
    Task AddAsync(ObraSocial obraSocial, CancellationToken cancellationToken);
    Task InvalidateCacheAsync(CancellationToken cancellationToken);
}
