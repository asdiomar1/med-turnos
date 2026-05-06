using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface ICondicionIvaRepository
{
    Task<IReadOnlyCollection<CondicionIva>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<CondicionIva?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<CondicionIva?> GetByNormalizedNameAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken);
    Task<int> GetNextOrderAsync(CancellationToken cancellationToken);
    Task AddAsync(CondicionIva condicionIva, CancellationToken cancellationToken);
    Task InvalidateCacheAsync(CancellationToken cancellationToken);
}
