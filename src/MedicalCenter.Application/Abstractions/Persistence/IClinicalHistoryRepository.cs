using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IClinicalHistoryRepository
{
    Task<ClinicalHistory?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ClinicalHistoryNumeroSummary>> GetResumenAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ClinicalEvolution>> GetEvolutionsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);
    Task AddAsync(ClinicalHistory history, CancellationToken cancellationToken);
    Task AddEvolutionAsync(ClinicalEvolution evolution, CancellationToken cancellationToken);
    Task<long> GetNextNumeroAsync(CancellationToken cancellationToken);
    Task<bool> IsNumeroTakenAsync(long numero, Guid excludePatientId, CancellationToken cancellationToken);
}
