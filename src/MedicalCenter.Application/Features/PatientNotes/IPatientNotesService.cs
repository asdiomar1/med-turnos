using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.PatientNotes;

public interface IPatientNotesService
{
    Task<IReadOnlyCollection<PatientNoteSummary>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken);
    Task<PatientNoteSummary> CreateAsync(Guid actorUserId, Guid patientId, string mensaje, CancellationToken cancellationToken);
    Task DeleteAsync(Guid actorUserId, Guid noteId, CancellationToken cancellationToken);
}
