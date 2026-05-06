using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.ClinicalHistory;

public sealed record CreateEvolutionCommand(
    int? MedicoId,
    DateOnly FechaClinica,
    string? Titulo,
    string Nota,
    string? DiagnosticoImpresion,
    string? Indicaciones,
    Guid? ConsultaSlotId,
    Guid? MedicoUserId = null);

public interface IClinicalHistoryService
{
    Task<IReadOnlyCollection<ClinicalHistoryNumeroSummary>> GetResumenAsync(CancellationToken cancellationToken);
    Task<ClinicalHistorySummary> GetAsync(Guid patientId, CancellationToken cancellationToken);
    Task<ClinicalHistorySummary> UpdateAsync(Guid actorUserId, Guid patientId, string? antecedentes, string? alergias, string? medicacionActual, string? observacionesRelevantes, CancellationToken cancellationToken);
    Task<ClinicalHistorySummary> UpdateNumeroAsync(Guid actorUserId, Guid patientId, long numero, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ClinicalEvolutionSummary>> GetEvolutionsAsync(Guid patientId, CancellationToken cancellationToken);
    Task<ClinicalEvolutionSummary> CreateEvolutionAsync(Guid actorUserId, Guid patientId, CreateEvolutionCommand command, CancellationToken cancellationToken);
}
