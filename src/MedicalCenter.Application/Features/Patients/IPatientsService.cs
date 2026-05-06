using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Patients;

public sealed record CreatePatientCommand(
    string Nombre,
    string? Email,
    string Telefono,
    string DocumentoIdentidad,
    string? LoginIdentifier,
    string? Nacionalidad,
    int CondicionIvaId,
    int? ObraSocialId,
    string? NumeroCredencialObraSocial,
    bool PortalHabilitado,
    bool OptInWhatsapp,
    string? OptInSource,
    bool Claustrofobico,
    string? Notas,
    string DatosExtra);

public sealed record UpdatePatientCommand(
    string? Email,
    string Telefono,
    string DocumentoIdentidad,
    string? Nacionalidad,
    int CondicionIvaId,
    int? ObraSocialId,
    string? NumeroCredencialObraSocial,
    bool Claustrofobico,
    string? Notas,
    string DatosExtra,
    bool ActualizarNotas,
    bool OptInWhatsapp,
    string? OptInSource);

public interface IPatientsService
{
    Task<IReadOnlyCollection<PatientSummary>> GetAsync(string? search, bool includeInactive, CancellationToken cancellationToken);
    Task<CreatedPatientResult> CreateAsync(Guid actorUserId, CreatePatientCommand command, CancellationToken cancellationToken);
    Task<PatientSummary> UpdateAsync(Guid actorUserId, Guid patientId, UpdatePatientCommand command, CancellationToken cancellationToken);
    Task<MutationResult> DeleteAsync(Guid patientId, CancellationToken cancellationToken);
    Task<PatientSummary> ConfigurePortalAsync(Guid patientId, bool portalHabilitado, CancellationToken cancellationToken);
    Task<PatientSummary> EnableResetAsync(Guid patientId, CancellationToken cancellationToken);
    Task<PatientSummary> UpdateMyDataAsync(Guid userId, string nombre, string? email, string telefono, CancellationToken cancellationToken);
}
