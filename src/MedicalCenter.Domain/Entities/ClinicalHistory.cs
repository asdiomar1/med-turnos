using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class ClinicalHistory : Entity<Guid>
{
    private ClinicalHistory() { }

    public ClinicalHistory(
        Guid patientId,
        long numero,
        string? antecedentes,
        string? alergias,
        string? medicacionActual,
        string? observacionesRelevantes,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        Id = patientId;
        PatientId = patientId;
        Numero = numero;
        Antecedentes = antecedentes;
        Alergias = alergias;
        MedicacionActual = medicacionActual;
        ObservacionesRelevantes = observacionesRelevantes;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
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
