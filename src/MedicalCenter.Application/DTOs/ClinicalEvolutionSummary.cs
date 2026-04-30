namespace MedicalCenter.Application.DTOs;

public sealed record ClinicalEvolutionSummary(
    Guid Id,
    Guid PatientId,
    Guid? ConsultaSlotId,
    int MedicoId,
    Guid AuthorProfileId,
    DateOnly FechaClinica,
    string? Titulo,
    string Nota,
    string? DiagnosticoImpresion,
    string? Indicaciones,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? MedicoNombre = null,
    bool MedicoActivo = false);
