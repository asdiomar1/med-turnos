namespace MedicalCenter.Application.DTOs;

public sealed record DashboardSummaryDto(
    int PacientesHoy,
    int ApartadosActivos);

public sealed record DashboardOccupancyCameraDto(
    int CamaraId,
    string CamaraNombre,
    int CapacidadTotal,
    int Ocupados,
    int PorcentajeOcupacion);

public sealed record DashboardAgendaRowDto(
    TimeOnly Hora,
    int Lugar,
    int CamaraId,
    string CamaraNombre,
    string NombrePaciente,
    string ModalidadCobro,
    bool EsNuevoIngreso,
    bool EsBloqueCompleto,
    string Estado);

public sealed record DashboardAlertDto(
    string Code,
    string Message,
    string Severity,
    int Count);

public sealed record DashboardUiAlertDto(
    string Tipo,
    string Titulo,
    string Descripcion,
    string TargetTab);

public sealed record DashboardWeeklyVolumeItemDto(
    DateOnly Fecha,
    int Ocupados);

public sealed record DailyClosingTurnoDto(
    Guid? SlotId,
    Guid? TurnoFueraHorarioId,
    int[]? SlotIds,
    Guid? PacienteId,
    DateOnly Fecha,
    string Hora,
    int? CamaraId,
    string? CamaraNombre,
    int PacienteNumeroDia,
    string? NombrePaciente,
    int SesionNumero,
    string ModalidadCobro,
    int? ObraSocialId,
    string? ObraSocialNombre,
    string? ObraSocialAbreviatura,
    decimal Importe,
    string? NumeroAutorizacion,
    int? SesionesAutorizadas,
    Guid? CicloObraSocialId,
    bool EsNuevoIngreso,
    int? MedicoId,
    string? MedicoNombre,
    bool EsMonoxido,
    bool EsOxibarica,
    bool Asistio,
    bool ReferidoTercero,
    int? ReferenteId,
    string? ReferenteNombre,
    string? ReferenteTipo);

public sealed record DailyClosingPreviewDto(
    DateOnly Fecha,
    int TotalTurnos,
    int Libres,
    int Ocupados,
    int Apartados,
    int Cancelados,
    decimal OcupacionPorcentaje,
    bool AptoParaCierre,
    IReadOnlyCollection<DashboardAlertDto> Alertas,
    DateTimeOffset GeneradoEn,
    IReadOnlyCollection<DailyClosingTurnoDto> Turnos);

public sealed record DailyClosingSummaryDto(
    Guid? Id,
    DateOnly Fecha,
    string Estado,
    string? DetallesJson,
    Guid? CreatedByUserId,
    Guid? ConfirmedByUserId,
    Guid? ReopenedByUserId,
    string? MotivoReapertura,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? ReopenedAt);
