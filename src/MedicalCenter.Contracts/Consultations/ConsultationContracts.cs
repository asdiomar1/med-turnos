using System.Text.Json.Serialization;
using MedicalCenter.Contracts.Common;

namespace MedicalCenter.Contracts.Consultations;

public sealed class ConsultationScheduleHourResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("hora")]
    public string Hora { get; init; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; init; }

    [JsonPropertyName("orden")]
    public int Orden { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ConsultationScheduleHourDeletionPreviewResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("hora")]
    public string Hora { get; init; } = string.Empty;

    [JsonPropertyName("can_delete")]
    public bool CanDelete { get; init; }

    [JsonPropertyName("future_slots_count")]
    public int FutureSlotsCount { get; init; }
}

public sealed class ConsultationSlotResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("paciente_id")]
    public Guid? PacienteId { get; init; }

    [JsonPropertyName("medico_id")]
    public int? MedicoId { get; init; }

    [JsonPropertyName("motivo_cancelacion")]
    public string? MotivoCancelacion { get; init; }

    [JsonPropertyName("observaciones_admin")]
    public string? ObservacionesAdmin { get; init; }

    [JsonPropertyName("confirmado_por")]
    public Guid? ConfirmadoPor { get; init; }

    [JsonPropertyName("confirmado_at")]
    public DateTimeOffset? ConfirmadoAt { get; init; }

    [JsonPropertyName("cerrado_por")]
    public Guid? CerradoPor { get; init; }

    [JsonPropertyName("cerrado_at")]
    public DateTimeOffset? CerradoAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("paciente")]
    public GuidLookupResponse? Paciente { get; init; }

    [JsonPropertyName("medico")]
    public IntLookupResponse? Medico { get; init; }

    [JsonPropertyName("confirmado_por_perfil")]
    public GuidLookupResponse? ConfirmadoPorPerfil { get; init; }

    [JsonPropertyName("cerrado_por_perfil")]
    public GuidLookupResponse? CerradoPorPerfil { get; init; }
}

public sealed class ConsultationSessionResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("paciente_id")]
    public Guid? PacienteId { get; init; }

    [JsonPropertyName("slot_id")]
    public Guid? SlotId { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("camara_id")]
    public int? CamaraId { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("modalidad_cobro")]
    public string ModalidadCobro { get; init; } = "particular";

    [JsonPropertyName("obra_social_id")]
    public int? ObraSocialId { get; init; }

    [JsonPropertyName("cierre_id")]
    public Guid? CierreId { get; init; }

    [JsonPropertyName("numero_autorizacion")]
    public string? NumeroAutorizacion { get; init; }

    [JsonPropertyName("sesiones_autorizadas")]
    public int? SesionesAutorizadas { get; init; }

    [JsonPropertyName("ciclo_obra_social_id")]
    public Guid? CicloObraSocialId { get; init; }
}

public sealed class ConsultationAvailabilityResponse
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("total_slots")]
    public int TotalSlots { get; init; }

    [JsonPropertyName("confirmados")]
    public int Confirmados { get; init; }

    [JsonPropertyName("libres")]
    public int Libres { get; init; }
}

public sealed class ConsultationAvailabilityDetailResponse
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("total_slots")]
    public int TotalSlots { get; init; }

    [JsonPropertyName("confirmados")]
    public int Confirmados { get; init; }

    [JsonPropertyName("libres")]
    public int Libres { get; init; }

    [JsonPropertyName("camara_id")]
    public int? CamaraId { get; init; }
}

public sealed class BlockHistoryPatientResponse
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
}

public sealed class BlockHistoryMedicoResponse
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
}

public sealed class BlockHistoryReferenteResponse
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;
}

public sealed class BlockHistoryObraSocialResponse
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
}

public sealed class BlockHistoryPerfilResponse
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
}

public sealed class BlockHistoryResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("camara_id")]
    public int? CamaraId { get; init; }

    [JsonPropertyName("slot_id")]
    public Guid? SlotId { get; init; }

    [JsonPropertyName("lugar")]
    public int? Lugar { get; init; }

    [JsonPropertyName("accion")]
    public string Accion { get; init; } = string.Empty;

    [JsonPropertyName("paciente_id")]
    public Guid? PacienteId { get; init; }

    [JsonPropertyName("realizado_por")]
    public Guid? RealizadoPor { get; init; }

    [JsonPropertyName("motivo")]
    public string? Motivo { get; init; }

    [JsonPropertyName("referido_tercero")]
    public bool ReferidoTercero { get; init; }

    [JsonPropertyName("modalidad_cobro")]
    public string ModalidadCobro { get; init; } = "particular";

    [JsonPropertyName("obra_social_id")]
    public int? ObraSocialId { get; init; }

    [JsonPropertyName("numero_autorizacion")]
    public string? NumeroAutorizacion { get; init; }

    [JsonPropertyName("obra_social_validada_por")]
    public Guid? ObraSocialValidadaPor { get; init; }

    [JsonPropertyName("obra_social_validada_at")]
    public DateTimeOffset? ObraSocialValidadaAt { get; init; }

    [JsonPropertyName("medico_id")]
    public int? MedicoId { get; init; }

    [JsonPropertyName("es_nuevo_ingreso")]
    public bool EsNuevoIngreso { get; init; }

    [JsonPropertyName("referente_id")]
    public int? ReferenteId { get; init; }

    [JsonPropertyName("tanda_id")]
    public Guid? TandaId { get; init; }

    [JsonPropertyName("sesiones_autorizadas")]
    public int? SesionesAutorizadas { get; init; }

    [JsonPropertyName("ciclo_obra_social_id")]
    public Guid? CicloObraSocialId { get; init; }

    [JsonPropertyName("paciente")]
    public BlockHistoryPatientResponse? Paciente { get; init; }

    [JsonPropertyName("medico")]
    public BlockHistoryMedicoResponse? Medico { get; init; }

    [JsonPropertyName("referente")]
    public BlockHistoryReferenteResponse? Referente { get; init; }

    [JsonPropertyName("obra_social")]
    public BlockHistoryObraSocialResponse? ObraSocial { get; init; }

    [JsonPropertyName("realizado_por_perfil")]
    public BlockHistoryPerfilResponse? RealizadoPorPerfil { get; init; }

    [JsonPropertyName("obra_social_validada_por_perfil")]
    public BlockHistoryPerfilResponse? ObraSocialValidadaPorPerfil { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ConsultationScheduleHourUpsertRequest
{
    [JsonPropertyName("hora")]
    public string Hora { get; init; } = string.Empty;

    [JsonPropertyName("orden")]
    public int Orden { get; init; }
}

public sealed class ToggleScheduleHourRequest
{
    [JsonPropertyName("activo")]
    public bool Activo { get; init; }
}

public sealed class AssignConsultationRequest
{
    [JsonPropertyName("paciente_id")]
    public Guid PacienteId { get; init; }

    [JsonPropertyName("medico_id")]
    public int MedicoId { get; init; }

    [JsonPropertyName("observaciones_admin")]
    public string? ObservacionesAdmin { get; init; }
}

public sealed class CancelConsultationRequest
{
    [JsonPropertyName("motivo")]
    public string? Motivo { get; init; }
}

public sealed class RescheduleConsultationRequest
{
    [JsonPropertyName("target_slot_id")]
    public Guid TargetSlotId { get; init; }

    [JsonPropertyName("medico_id")]
    public int? MedicoId { get; init; }
}

public sealed class CloseConsultationRequest
{
    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("titulo")]
    public string? Titulo { get; init; }

    [JsonPropertyName("nota")]
    public string? Nota { get; init; }

    [JsonPropertyName("diagnostico_impresion")]
    public string? DiagnosticoImpresion { get; init; }

    [JsonPropertyName("indicaciones")]
    public string? Indicaciones { get; init; }
}

public sealed class OutOfHoursTurnCreateRequest
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("paciente_id")]
    public Guid PacienteId { get; init; }

    [JsonPropertyName("operador_camara_id")]
    public Guid? OperadorCamaraId { get; init; }

    [JsonPropertyName("notas")]
    public string? Notas { get; init; }

    [JsonPropertyName("es_monoxido")]
    public bool EsMonoxido { get; init; }

    [JsonPropertyName("monoxido_orden_medica")]
    public bool MonoxidoOrdenMedica { get; init; }

    [JsonPropertyName("monoxido_resumen_clinico")]
    public bool MonoxidoResumenClinico { get; init; }

    [JsonPropertyName("monoxido_medico_id")]
    public int? MonoxidoMedicoId { get; init; }
}

public sealed class OutOfHoursTurnResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("paciente_id")]
    public Guid PacienteId { get; init; }

    [JsonPropertyName("notas")]
    public string? Notas { get; init; }

    [JsonPropertyName("creado_por")]
    public Guid CreadoPor { get; init; }

    [JsonPropertyName("operador_camara_id")]
    public Guid OperadorCamaraId { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("es_monoxido")]
    public bool EsMonoxido { get; init; }

    [JsonPropertyName("monoxido_orden_medica")]
    public bool MonoxidoOrdenMedica { get; init; }

    [JsonPropertyName("monoxido_resumen_clinico")]
    public bool MonoxidoResumenClinico { get; init; }

    [JsonPropertyName("monoxido_medico_id")]
    public int? MonoxidoMedicoId { get; init; }

    [JsonPropertyName("paciente")]
    public GuidLookupResponse? Paciente { get; init; }

    [JsonPropertyName("monoxido_medico")]
    public IntLookupResponse? MonoxidoMedico { get; init; }

    [JsonPropertyName("operador_camara")]
    public GuidLookupResponse? OperadorCamara { get; init; }
}

public sealed class GenerateConsultationsRequest
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }
}

public sealed class RepairConsultationsRangeRequest
{
    [JsonPropertyName("fecha_inicio")]
    public DateOnly FechaInicio { get; init; }

    [JsonPropertyName("fecha_fin")]
    public DateOnly FechaFin { get; init; }
}
