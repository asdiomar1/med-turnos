using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface ICameraRepository
{
    Task<IReadOnlyCollection<Camera>> GetAsync(CancellationToken cancellationToken);
    Task<Camera?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> GetNextIdAsync(CancellationToken cancellationToken);
    Task AddAsync(Camera camera, CancellationToken cancellationToken);
}
