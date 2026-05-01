using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Patients;

public interface IPatientsService
{
    Task<IReadOnlyCollection<PatientSummary>> GetAsync(string? search, bool includeInactive, CancellationToken cancellationToken);
    Task<CreatedPatientResult> CreateAsync(
        Guid actorUserId,
        string nombre,
        string? email,
        string telefono,
        string documentoIdentidad,
        string? loginIdentifier,
        string? nacionalidad,
        int condicionIvaId,
        int? obraSocialId,
        string? numeroCredencialObraSocial,
        bool portalHabilitado,
        bool optInWhatsapp,
        string? optInSource,
        bool claustrofobico,
        string? notas,
        string datosExtra,
        CancellationToken cancellationToken);
    Task<PatientSummary> UpdateAsync(
        Guid actorUserId,
        Guid patientId,
        string? email,
        string telefono,
        string documentoIdentidad,
        string? nacionalidad,
        int condicionIvaId,
        int? obraSocialId,
        string? numeroCredencialObraSocial,
        bool claustrofobico,
        string? notas,
        string datosExtra,
        bool actualizarNotas,
        bool optInWhatsapp,
        string? optInSource,
        CancellationToken cancellationToken);
    Task<MutationResult> DeleteAsync(Guid patientId, CancellationToken cancellationToken);
    Task<PatientSummary> ConfigurePortalAsync(Guid patientId, bool portalHabilitado, CancellationToken cancellationToken);
    Task<PatientSummary> EnableResetAsync(Guid patientId, CancellationToken cancellationToken);
    Task<PatientSummary> UpdateMyDataAsync(Guid userId, string nombre, string? email, string telefono, CancellationToken cancellationToken);
}
