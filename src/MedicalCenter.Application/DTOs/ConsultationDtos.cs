namespace MedicalCenter.Application.DTOs;

public sealed record GuidLookupSummary(
    Guid Id,
    string Nombre,
    string? DocumentoIdentidad = null,
    string? Email = null,
    bool? Activo = null);

public sealed record IntLookupSummary(
    int Id,
    string Nombre,
    string? Extra = null,
    bool? Activo = null);

public sealed record ConsultationScheduleHourSummary(
    int Id,
    string Hora,
    bool Activo,
    int Orden,
    DateTimeOffset CreatedAt);

public sealed record ConsultationScheduleHourDeletionPreviewSummary(
    int Id,
    string Hora,
    bool CanDelete,
    int FutureSlotsCount);

public sealed record ConsultationSlotSummary(
    Guid Id,
    DateOnly Fecha,
    TimeOnly Hora,
    string Estado,
    Guid? PacienteId,
    int? MedicoId,
    Guid? MedicoUserId,
    string? MedicoNombre,
    string? MotivoCancelacion,
    string? ObservacionesAdmin,
    Guid? ConfirmadoPor,
    DateTimeOffset? ConfirmadoAt,
    Guid? CerradoPor,
    DateTimeOffset? CerradoAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    GuidLookupSummary? Paciente,
    IntLookupSummary? Medico,
    GuidLookupSummary? MedicoUser,
    GuidLookupSummary? ConfirmadoPorPerfil,
    GuidLookupSummary? CerradoPorPerfil);

public sealed record ConsultationSessionSummary(
    Guid Id,
    Guid? PacienteId,
    Guid? SlotId,
    DateOnly Fecha,
    TimeOnly Hora,
    int? CamaraId,
    DateTimeOffset CreatedAt,
    string ModalidadCobro,
    int? ObraSocialId,
    Guid? CierreId,
    string? NumeroAutorizacion,
    int? SesionesAutorizadas,
    Guid? CicloObraSocialId);

public sealed record ConsultationAvailabilitySummary(
    DateOnly Fecha,
    int TotalSlots,
    int Confirmados,
    int Libres,
    Guid? TandaId = null);

public sealed record ConsultationAvailabilityDetailSummary(
    DateOnly Fecha,
    TimeOnly Hora,
    int TotalSlots,
    int Confirmados,
    int Libres,
    int? CamaraId = null,
    Guid? TandaId = null);

public sealed record BlockHistorySummary(
    Guid Id,
    DateOnly Fecha,
    TimeOnly Hora,
    int? CamaraId,
    Guid? SlotId,
    int? Lugar,
    string Accion,
    Guid? PacienteId,
    Guid? RealizadoPor,
    string? Motivo,
    bool ReferidoTercero,
    string ModalidadCobro,
    int? ObraSocialId,
    string? NumeroAutorizacion,
    Guid? ObraSocialValidadaPor,
    DateTimeOffset? ObraSocialValidadaAt,
    int? MedicoId,
    Guid? MedicoUserId,
    string? MedicoNombre,
    bool EsNuevoIngreso,
    int? ReferenteId,
    Guid? TandaId,
    int? SesionesAutorizadas,
    Guid? CicloObraSocialId,
    DateTimeOffset CreatedAt,
    GuidLookupSummary? Paciente,
    IntLookupSummary? Medico,
    IntLookupSummary? Referente,
    ObraSocialSummaryDto? ObraSocial,
    GuidLookupSummary? RealizadoPorPerfil,
    GuidLookupSummary? ObraSocialValidadaPorPerfil);

public sealed record ConsultationOperativeCommand(
    bool ReferidoTercero,
    int? ReferenteId,
    string? ModalidadCobro,
    int? ObraSocialId,
    string? NumeroAutorizacion,
    int? SesionesAutorizadas,
    Guid? CicloObraSocialId,
    bool IniciarNuevoCicloObraSocial,
    bool ConvenioCorroborado,
    int? MedicoId,
    bool EsNuevoIngreso,
    bool EsMonoxido,
    bool MonoxidoOrdenMedica,
    bool MonoxidoResumenClinico);

public sealed record ConsultationScheduleHourUpsertCommand(string Hora, int Orden);
public sealed record AssignConsultationCommand(Guid PacienteId, int? MedicoId, string? ObservacionesAdmin, Guid? MedicoUserId = null);
public sealed record CancelConsultationCommand(string? Motivo);
public sealed record RescheduleConsultationCommand(Guid TargetSlotId, int? MedicoId, Guid? MedicoUserId = null);
public sealed record CloseConsultationCommand(string Estado, string? Titulo, string? Nota, string? DiagnosticoImpresion, string? Indicaciones);
public sealed record GenerateConsultationsCommand(DateOnly Fecha);
public sealed record RepairConsultationsRangeCommand(DateOnly FechaInicio, DateOnly FechaFin);
public sealed record OutOfHoursTurnCreateCommand(
    DateOnly Fecha,
    TimeOnly Hora,
    Guid PacienteId,
    Guid? OperadorCamaraId,
    string? Notas,
    bool EsMonoxido,
    bool MonoxidoOrdenMedica,
    bool MonoxidoResumenClinico,
    int? MonoxidoMedicoId,
    Guid? MonoxidoMedicoUserId = null,
    int? MedicoId = null,
    Guid? MedicoUserId = null);

public sealed record OutOfHoursTurnSummary(
    Guid Id,
    DateOnly Fecha,
    TimeOnly Hora,
    Guid PacienteId,
    string? Notas,
    Guid CreadoPor,
    Guid OperadorCamaraId,
    DateTimeOffset? CreatedAt,
    bool EsMonoxido,
    bool MonoxidoOrdenMedica,
    bool MonoxidoResumenClinico,
    int? MonoxidoMedicoId,
    Guid? MonoxidoMedicoUserId,
    GuidLookupSummary? Paciente,
    IntLookupSummary? MonoxidoMedico,
    GuidLookupSummary? MonoxidoMedicoUser,
    GuidLookupSummary? OperadorCamara);
