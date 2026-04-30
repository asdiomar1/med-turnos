using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IPatientRepository
{
    Task<IReadOnlyCollection<Patient>> GetAsync(string? search, bool includeInactive, CancellationToken cancellationToken);
    Task<Patient?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken);
    Task<Patient?> GetByDocumentoAsync(string documentoIdentidad, CancellationToken cancellationToken);
    Task<Patient?> GetByLoginIdentifierAsync(string loginIdentifier, CancellationToken cancellationToken);
    Task<Patient?> GetByPortalIdentifierAsync(string identifier, CancellationToken cancellationToken);
    Task AddAsync(Patient patient, CancellationToken cancellationToken);
}
