using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<IReadOnlyCollection<Role>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken);
    Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Role role, CancellationToken cancellationToken);
}
