namespace MedicalCenter.Application.DTOs;

public sealed record DashboardSummaryDto(
    DateOnly Fecha,
    int TotalTurnos,
    int Libres,
    int Ocupados,
    int Apartados,
    int Cancelados,
    decimal OcupacionPorcentaje,
    DateTimeOffset GeneradoEn);

public sealed record DashboardOccupancyCameraDto(
    int? CameraId,
    string? CameraName,
    int TotalTurnos,
    int Libres,
    int Ocupados,
    int Apartados,
    int Cancelados);

public sealed record DashboardOccupancyDto(
    DateOnly Fecha,
    int TotalTurnos,
    int Libres,
    int Ocupados,
    int Apartados,
    int Cancelados,
    decimal OcupacionPorcentaje,
    IReadOnlyCollection<DashboardOccupancyCameraDto> PorCamara);

public sealed record DashboardAgendaBucketDto(
    DateOnly Fecha,
    TimeOnly Hora,
    int? CameraId,
    string? CameraName,
    int TotalTurnos,
    int Libres,
    int Ocupados,
    int Apartados,
    int Cancelados);

public sealed record DashboardAlertDto(
    string Code,
    string Message,
    string Severity,
    int Count);

public sealed record DashboardWeeklyVolumeItemDto(
    DateOnly Fecha,
    int TotalTurnos,
    int Libres,
    int Ocupados,
    int Apartados,
    int Cancelados);

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
    string? ModalidadCobro,
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
    bool Asistio);

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
