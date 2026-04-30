namespace MedicalCenter.Application.DTOs;

public sealed record ClinicalHistorySummary(
    Guid PatientId,
    long Numero,
    string? Antecedentes,
    string? Alergias,
    string? MedicacionActual,
    string? ObservacionesRelevantes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ClinicalHistoryNumeroSummary(
    Guid PatientId,
    long Numero);
