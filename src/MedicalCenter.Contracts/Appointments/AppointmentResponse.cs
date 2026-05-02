using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Appointments;

public sealed class AppointmentResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("lugar")]
    public int Lugar { get; init; }

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("paciente_id")]
    public Guid? PacienteId { get; init; }

    [JsonPropertyName("camara_id")]
    public int? CamaraId { get; init; }

    [JsonPropertyName("camara")]
    public AppointmentCameraResponse? Camara { get; init; }

    [JsonPropertyName("block_id")]
    public Guid? BlockId { get; init; }

    [JsonPropertyName("tanda_id")]
    public Guid? TandaId { get; init; }

    [JsonPropertyName("apartado_por_user_id")]
    public Guid? ApartadoPorUserId { get; init; }

    [JsonPropertyName("apartado_ts")]
    public DateTimeOffset? ApartadoTs { get; init; }

    [JsonPropertyName("es_bloque_completo")]
    public bool EsBloqueCompleto { get; init; }

    [JsonPropertyName("es_tanda")]
    public bool EsTanda { get; init; }

    [JsonPropertyName("referido_tercero")]
    public bool ReferidoTercero { get; init; }

    [JsonPropertyName("referente_id")]
    public int? ReferenteId { get; init; }

    [JsonPropertyName("modalidad_cobro")]
    public string ModalidadCobro { get; init; } = "particular";

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

    [JsonPropertyName("medico_user_id")]
    public Guid? MedicoUserId { get; init; }

    [JsonPropertyName("es_nuevo_ingreso")]
    public bool EsNuevoIngreso { get; init; }

    [JsonPropertyName("es_monoxido")]
    public bool EsMonoxido { get; init; }

    [JsonPropertyName("monoxido_orden_medica")]
    public bool MonoxidoOrdenMedica { get; init; }

    [JsonPropertyName("monoxido_resumen_clinico")]
    public bool MonoxidoResumenClinico { get; init; }
}

public sealed class AppointmentCameraResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("capacidad")]
    public int Capacidad { get; init; }
}
