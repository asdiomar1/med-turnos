using System.Text.Json;
using System.Text.Json.Serialization;
using MedicalCenter.Contracts.Dashboards;

namespace MedicalCenter.Contracts.DailyClosings;

public sealed class DailyClosingTurnoResponse
{
    [JsonPropertyName("slot_id")]
    public Guid? SlotId { get; init; }

    [JsonPropertyName("turno_fuera_horario_id")]
    public Guid? TurnoFueraHorarioId { get; init; }

    [JsonPropertyName("slot_ids")]
    public int[]? SlotIds { get; init; }

    [JsonPropertyName("paciente_id")]
    public Guid? PacienteId { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public string Hora { get; init; } = string.Empty;

    [JsonPropertyName("camara_id")]
    public int? CamaraId { get; init; }

    [JsonPropertyName("camara_nombre")]
    public string? CamaraNombre { get; init; }

    [JsonPropertyName("paciente_numero_dia")]
    public int PacienteNumeroDia { get; init; }

    [JsonPropertyName("nombre_paciente")]
    public string? NombrePaciente { get; init; }

    [JsonPropertyName("sesion_numero")]
    public int SesionNumero { get; init; }

    [JsonPropertyName("modalidad_cobro")]
    public string? ModalidadCobro { get; init; }

    [JsonPropertyName("obra_social_id")]
    public int? ObraSocialId { get; init; }

    [JsonPropertyName("obra_social_nombre")]
    public string? ObraSocialNombre { get; init; }

    [JsonPropertyName("obra_social_abreviatura")]
    public string? ObraSocialAbreviatura { get; init; }

    [JsonPropertyName("importe")]
    public decimal Importe { get; init; }

    [JsonPropertyName("numero_autorizacion")]
    public string? NumeroAutorizacion { get; init; }

    [JsonPropertyName("sesiones_autorizadas")]
    public int? SesionesAutorizadas { get; init; }

    [JsonPropertyName("ciclo_obra_social_id")]
    public Guid? CicloObraSocialId { get; init; }

    [JsonPropertyName("es_nuevo_ingreso")]
    public bool EsNuevoIngreso { get; init; }

    [JsonPropertyName("medico_id")]
    public int? MedicoId { get; init; }

    [JsonPropertyName("medico_nombre")]
    public string? MedicoNombre { get; init; }

    [JsonPropertyName("es_monoxido")]
    public bool EsMonoxido { get; init; }

    [JsonPropertyName("es_oxibarica")]
    public bool EsOxibarica { get; init; }

    [JsonPropertyName("asistio")]
    public bool Asistio { get; init; }
}

public sealed class DailyClosingPreviewResponse
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("total_turnos")]
    public int TotalTurnos { get; init; }

    [JsonPropertyName("libres")]
    public int Libres { get; init; }

    [JsonPropertyName("ocupados")]
    public int Ocupados { get; init; }

    [JsonPropertyName("apartados")]
    public int Apartados { get; init; }

    [JsonPropertyName("cancelados")]
    public int Cancelados { get; init; }

    [JsonPropertyName("ocupacion_porcentaje")]
    public decimal OcupacionPorcentaje { get; init; }

    [JsonPropertyName("apto_para_cierre")]
    public bool AptoParaCierre { get; init; }

    [JsonPropertyName("alertas")]
    public IReadOnlyCollection<DashboardAlertResponse> Alertas { get; init; } = [];

    [JsonPropertyName("generado_en")]
    public DateTimeOffset GeneradoEn { get; init; }

    [JsonPropertyName("turnos")]
    public IReadOnlyCollection<DailyClosingTurnoResponse> Turnos { get; init; } = [];
}

public sealed class DailyClosingResponse
{
    [JsonPropertyName("id")]
    public Guid? Id { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("detalles")]
    public JsonElement? Detalles { get; init; }

    [JsonPropertyName("created_by_user_id")]
    public Guid? CreatedByUserId { get; init; }

    [JsonPropertyName("confirmed_by_user_id")]
    public Guid? ConfirmedByUserId { get; init; }

    [JsonPropertyName("reopened_by_user_id")]
    public Guid? ReopenedByUserId { get; init; }

    [JsonPropertyName("motivo_reapertura")]
    public string? MotivoReapertura { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("confirmed_at")]
    public DateTimeOffset? ConfirmedAt { get; init; }

    [JsonPropertyName("reopened_at")]
    public DateTimeOffset? ReopenedAt { get; init; }
}

public sealed class ConfirmDailyClosingRequest
{
    [JsonPropertyName("detalles")]
    public JsonElement? Detalles { get; init; }
}

public sealed class ReopenDailyClosingRequest
{
    [JsonPropertyName("motivo")]
    public string? Motivo { get; init; }
}

public sealed class PreviewDailyClosingRequest
{
    [JsonPropertyName("fecha")]
    public DateOnly? Fecha { get; init; }
}
