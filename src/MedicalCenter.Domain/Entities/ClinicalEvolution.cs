using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class ClinicalEvolution : Entity<Guid>
{
    private ClinicalEvolution() { }

    public ClinicalEvolution(ClinicalEvolutionCreateData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        Id = data.Id;
        PatientId = data.PatientId;
        ConsultaSlotId = data.ConsultaSlotId;
        MedicoId = data.MedicoId;
        MedicoUserId = data.MedicoUserId;
        AuthorProfileId = data.AuthorProfileId;
        FechaClinica = data.FechaClinica;
        Titulo = data.Titulo;
        Nota = data.Nota;
        DiagnosticoImpresion = data.DiagnosticoImpresion;
        Indicaciones = data.Indicaciones;
        CreatedAt = data.CreatedAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = data.UpdatedAt ?? DateTimeOffset.UtcNow;
    }

    public Guid PatientId { get; private set; }
    public Guid? ConsultaSlotId { get; private set; }
    public int MedicoId { get; private set; }
    public Guid? MedicoUserId { get; private set; }
    public Guid AuthorProfileId { get; private set; }
    public DateOnly FechaClinica { get; private set; }
    public string? Titulo { get; private set; }
    public string Nota { get; private set; } = string.Empty;
    public string? DiagnosticoImpresion { get; private set; }
    public string? Indicaciones { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}

public sealed class ClinicalEvolutionCreateData
{
    public required Guid Id { get; init; }
    public required Guid PatientId { get; init; }
    public Guid? ConsultaSlotId { get; init; }
    public required int MedicoId { get; init; }
    public Guid? MedicoUserId { get; init; }
    public required Guid AuthorProfileId { get; init; }
    public required DateOnly FechaClinica { get; init; }
    public string? Titulo { get; init; }
    public required string Nota { get; init; }
    public string? DiagnosticoImpresion { get; init; }
    public string? Indicaciones { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
