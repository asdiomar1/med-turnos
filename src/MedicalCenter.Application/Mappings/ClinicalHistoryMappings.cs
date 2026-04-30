using MedicalCenter.Domain.Entities;
using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Mappings;

public static class ClinicalHistoryMappings
{
    public static ClinicalHistorySummary ToSummary(this ClinicalHistory x) =>
        new(
            x.PatientId,
            x.Numero,
            x.Antecedentes,
            x.Alergias,
            x.MedicacionActual,
            x.ObservacionesRelevantes,
            x.CreatedAt,
            x.UpdatedAt);

    public static ClinicalEvolutionSummary ToSummary(this ClinicalEvolution x, string? medicoNombre = null, bool medicoActivo = false) =>
        new(
            x.Id,
            x.PatientId,
            x.ConsultaSlotId,
            x.MedicoId,
            x.AuthorProfileId,
            x.FechaClinica,
            x.Titulo,
            x.Nota,
            x.DiagnosticoImpresion,
            x.Indicaciones,
            x.CreatedAt,
            x.UpdatedAt,
            medicoNombre,
            medicoActivo);
}