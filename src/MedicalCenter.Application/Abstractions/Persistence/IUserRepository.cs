using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);
    Task<Guid?> GetProfileIdByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken);
    Task<string?> GetDisplayNameByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<User>> GetStaffAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<User>> GetByRoleAsync(string roleCode, bool onlyActive, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<User>> GetBasicByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}
