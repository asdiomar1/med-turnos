using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.ClinicalHistory;

public sealed class ClinicalHistoryResponse
{
    [JsonPropertyName("paciente_id")]
    public Guid PatientId { get; init; }

    [JsonPropertyName("numero")]
    public long Numero { get; init; }

    [JsonPropertyName("antecedentes")]
    public string? Antecedentes { get; init; }

    [JsonPropertyName("alergias")]
    public string? Alergias { get; init; }

    [JsonPropertyName("medicacion_actual")]
    public string? MedicacionActual { get; init; }

    [JsonPropertyName("observaciones_relevantes")]
    public string? ObservacionesRelevantes { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class ClinicalHistoryResumenResponse
{
    [JsonPropertyName("paciente_id")]
    public Guid PatientId { get; init; }

    [JsonPropertyName("numero")]
    public long Numero { get; init; }
}

public sealed class UpdateClinicalHistoryRequest
{
    [JsonPropertyName("antecedentes")]
    public string? Antecedentes { get; init; }

    [JsonPropertyName("alergias")]
    public string? Alergias { get; init; }

    [JsonPropertyName("medicacion_actual")]
    public string? MedicacionActual { get; init; }

    [JsonPropertyName("observaciones_relevantes")]
    public string? ObservacionesRelevantes { get; init; }
}

public sealed class UpdateClinicalHistoryNumberRequest
{
    [JsonPropertyName("numero")]
    public long Numero { get; init; }
}

public sealed class ClinicalEvolutionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("paciente_id")]
    public Guid PatientId { get; init; }

    [JsonPropertyName("consulta_slot_id")]
    public Guid? ConsultaSlotId { get; init; }

    [JsonPropertyName("medico_id")]
    public int MedicoId { get; init; }

    [JsonPropertyName("medico_user_id")]
    public Guid? MedicoUserId { get; init; }

    [JsonPropertyName("autor_perfil_id")]
    public Guid AuthorProfileId { get; init; }

    [JsonPropertyName("fecha_clinica")]
    public DateOnly FechaClinica { get; init; }

    [JsonPropertyName("titulo")]
    public string? Titulo { get; init; }

    [JsonPropertyName("nota")]
    public string Nota { get; init; } = string.Empty;

    [JsonPropertyName("diagnostico_impresion")]
    public string? DiagnosticoImpresion { get; init; }

    [JsonPropertyName("indicaciones")]
    public string? Indicaciones { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("medico_nombre")]
    public string? MedicoNombre { get; init; }

    [JsonPropertyName("medico_activo")]
    public bool MedicoActivo { get; init; }
}

public sealed class CreateClinicalEvolutionRequest
{
    [JsonPropertyName("medico_id")]
    public int? MedicoId { get; init; }

    [JsonPropertyName("medico_user_id")]
    public Guid? MedicoUserId { get; init; }

    [JsonPropertyName("fecha_clinica")]
    public DateOnly FechaClinica { get; init; }

    [JsonPropertyName("titulo")]
    public string? Titulo { get; init; }

    [JsonPropertyName("nota")]
    public string Nota { get; init; } = string.Empty;

    [JsonPropertyName("diagnostico_impresion")]
    public string? DiagnosticoImpresion { get; init; }

    [JsonPropertyName("indicaciones")]
    public string? Indicaciones { get; init; }

    [JsonPropertyName("consulta_slot_id")]
    public Guid? ConsultaSlotId { get; init; }
}
