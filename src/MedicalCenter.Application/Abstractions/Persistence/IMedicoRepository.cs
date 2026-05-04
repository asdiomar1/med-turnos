using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IMedicoRepository
{
    Task<Medico?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Medico>> GetAsync(bool onlyActive, CancellationToken cancellationToken);
    Task<Medico?> GetByNormalizedNameAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Medico>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken);
    Task<int> GetNextOrderAsync(CancellationToken cancellationToken);
    Task AddAsync(Medico medico, CancellationToken cancellationToken);
}
