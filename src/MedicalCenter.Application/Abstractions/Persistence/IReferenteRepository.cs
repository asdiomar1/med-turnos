using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IReferenteRepository
{
    Task<Referente?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Referente>> GetAsync(CancellationToken cancellationToken);
    Task<Referente?> GetByNormalizedNameAndTypeAsync(string normalizedName, string normalizedType, int? exceptId, CancellationToken cancellationToken);
    Task<int> GetNextOrderAsync(CancellationToken cancellationToken);
    Task AddAsync(Referente referente, CancellationToken cancellationToken);
}
