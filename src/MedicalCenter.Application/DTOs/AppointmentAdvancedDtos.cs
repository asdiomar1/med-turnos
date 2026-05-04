namespace MedicalCenter.Application.DTOs;

public sealed record AppointmentOperativeCommand(
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
    bool MonoxidoResumenClinico,
    Guid? MedicoUserId = null);

public sealed record TandaAvailabilitySummary(
    DateOnly Fecha,
    int TotalSlots,
    int Ocupados,
    int Libres,
    Guid? TandaId = null);

public sealed record TandaAvailabilityDetailSummary(
    DateOnly Fecha,
    TimeOnly Hora,
    int CamaraId,
    int Lugar,
    string Estado,
    Guid? TandaId,
    Guid? PacienteId,
    bool EsBloqueCompleto);

public sealed record BlockHistoryWriteCommand(
    DateOnly Fecha,
    TimeOnly Hora,
    int? CamaraId,
    Guid? SlotId,
    int? Lugar,
    string Accion,
    Guid? PacienteId,
    string? Motivo);

/// <summary>
/// Aggregated availability summary for camera-based appointment blocks.
/// Groups slots by fecha + hora + camara_id with availability counts and patient blocking info.
/// </summary>
public sealed record TandaAvailabilityAggregatedSummary(
    DateOnly Fecha,
    TimeOnly Hora,
    int CamaraId,
    string CamaraNombre,
    int Capacidad,
    int LibresCount,
    bool TieneDisponibilidad,
    bool TieneBloqueCompletoPosible,
    bool BloqueadoPorPaciente);
