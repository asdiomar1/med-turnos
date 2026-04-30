using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class ClinicalEvolution : Entity<Guid>
{
    private ClinicalEvolution() { }

    public ClinicalEvolution(
        Guid id,
        Guid patientId,
        Guid? consultaSlotId,
        int medicoId,
        Guid authorProfileId,
        DateOnly fechaClinica,
        string? titulo,
        string nota,
        string? diagnosticoImpresion,
        string? indicaciones,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        Id = id;
        PatientId = patientId;
        ConsultaSlotId = consultaSlotId;
        MedicoId = medicoId;
        AuthorProfileId = authorProfileId;
        FechaClinica = fechaClinica;
        Titulo = titulo;
        Nota = nota;
        DiagnosticoImpresion = diagnosticoImpresion;
        Indicaciones = indicaciones;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
    }

    public Guid PatientId { get; private set; }
    public Guid? ConsultaSlotId { get; private set; }
    public int MedicoId { get; private set; }
    public Guid AuthorProfileId { get; private set; }
    public DateOnly FechaClinica { get; private set; }
    public string? Titulo { get; private set; }
    public string Nota { get; private set; } = string.Empty;
    public string? DiagnosticoImpresion { get; private set; }
    public string? Indicaciones { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}
