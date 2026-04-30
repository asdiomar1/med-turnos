using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);
    Task<Guid?> GetProfileIdByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<User>> GetStaffAsync(bool includeInactive, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}
