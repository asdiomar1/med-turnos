using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.ClinicalHistory;

public interface IClinicalHistoryService
{
    Task<IReadOnlyCollection<ClinicalHistoryNumeroSummary>> GetResumenAsync(CancellationToken cancellationToken);
    Task<ClinicalHistorySummary> GetAsync(Guid patientId, CancellationToken cancellationToken);
    Task<ClinicalHistorySummary> UpdateAsync(Guid actorUserId, Guid patientId, string? antecedentes, string? alergias, string? medicacionActual, string? observacionesRelevantes, CancellationToken cancellationToken);
    Task<ClinicalHistorySummary> UpdateNumeroAsync(Guid actorUserId, Guid patientId, long numero, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ClinicalEvolutionSummary>> GetEvolutionsAsync(Guid patientId, CancellationToken cancellationToken);
    Task<ClinicalEvolutionSummary> CreateEvolutionAsync(Guid actorUserId, Guid patientId, int medicoId, DateOnly fechaClinica, string? titulo, string nota, string? diagnosticoImpresion, string? indicaciones, Guid? consultaSlotId, CancellationToken cancellationToken);
}
