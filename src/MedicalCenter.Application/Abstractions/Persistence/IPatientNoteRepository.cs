using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IPatientNoteRepository
{
    Task<IReadOnlyCollection<PatientNote>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);
    Task<PatientNote?> GetByIdAsync(Guid noteId, CancellationToken cancellationToken);
    Task AddAsync(PatientNote note, CancellationToken cancellationToken);
    Task DeleteAsync(PatientNote note, CancellationToken cancellationToken);
}
