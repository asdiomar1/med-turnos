using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed record ClinicalHistoryCreateParams(
    Guid PatientId,
    long Numero,
    string? Antecedentes,
    string? Alergias,
    string? MedicacionActual,
    string? ObservacionesRelevantes,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null);

public sealed class ClinicalHistory : Entity<Guid>
{
    private ClinicalHistory() { }

    public ClinicalHistory(ClinicalHistoryCreateParams p)
    {
        Id = p.PatientId;
        PatientId = p.PatientId;
        Numero = p.Numero;
        Antecedentes = p.Antecedentes;
        Alergias = p.Alergias;
        MedicacionActual = p.MedicacionActual;
        ObservacionesRelevantes = p.ObservacionesRelevantes;
        CreatedAt = p.CreatedAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = p.UpdatedAt ?? DateTimeOffset.UtcNow;
    }

    public Guid PatientId { get; private set; }
    public long Numero { get; private set; }
    public string? Antecedentes { get; private set; }
    public string? Alergias { get; private set; }
    public string? MedicacionActual { get; private set; }
    public string? ObservacionesRelevantes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string? antecedentes, string? alergias, string? medicacionActual, string? observacionesRelevantes)
    {
        Antecedentes = antecedentes;
        Alergias = alergias;
        MedicacionActual = medicacionActual;
        ObservacionesRelevantes = observacionesRelevantes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetNumero(long numero)
    {
        Numero = numero;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
