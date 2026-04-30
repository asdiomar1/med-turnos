using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Appointments;

public sealed class AssignAppointmentRequest
{
    [JsonPropertyName("paciente_id")]
    public Guid PacienteId { get; init; }

    [JsonPropertyName("es_tanda")]
    public bool EsTanda { get; init; }

    [JsonPropertyName("tanda_id")]
    public Guid? TandaId { get; init; }

    [JsonPropertyName("accion")]
    public string? Accion { get; init; }

    [JsonPropertyName("referido_tercero")]
    public bool ReferidoTercero { get; init; }

    [JsonPropertyName("referente_id")]
    public int? ReferenteId { get; init; }

    [JsonPropertyName("modalidad_cobro")]
    public string? ModalidadCobro { get; init; }

    [JsonPropertyName("obra_social_id")]
    public int? ObraSocialId { get; init; }

    [JsonPropertyName("numero_autorizacion")]
    public string? NumeroAutorizacion { get; init; }

    [JsonPropertyName("sesiones_autorizadas")]
    public int? SesionesAutorizadas { get; init; }

    [JsonPropertyName("ciclo_obra_social_id")]
    public Guid? CicloObraSocialId { get; init; }

    [JsonPropertyName("iniciar_nuevo_ciclo_obra_social")]
    public bool IniciarNuevoCicloObraSocial { get; init; }

    [JsonPropertyName("convenio_corroborado")]
    public bool ConvenioCorroborado { get; init; }

    [JsonPropertyName("medico_id")]
    public int? MedicoId { get; init; }

    [JsonPropertyName("es_nuevo_ingreso")]
    public bool EsNuevoIngreso { get; init; }

    [JsonPropertyName("es_monoxido")]
    public bool EsMonoxido { get; init; }

    [JsonPropertyName("monoxido_orden_medica")]
    public bool MonoxidoOrdenMedica { get; init; }

    [JsonPropertyName("monoxido_resumen_clinico")]
    public bool MonoxidoResumenClinico { get; init; }
}
