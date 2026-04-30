using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Appointments;

public interface IAppointmentsService
{
    Task<IReadOnlyCollection<AppointmentSummary>> GetByDateAsync(DateOnly? fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AppointmentSummary>> GetDisponiblesPortalByDateAsync(Guid userId, DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AppointmentGroupSummary>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AppointmentSummary>> GetActivosByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken);
    Task<AppointmentSummary> AssignAsync(Guid actorUserId, Guid slotId, string idempotencyKey, AssignAppointmentCommand command, CancellationToken cancellationToken);
    Task<AppointmentSummary> CancelAsync(Guid actorUserId, Guid slotId, string idempotencyKey, string? motivo, CancellationToken cancellationToken);
    Task<AppointmentSummary> RescheduleAsync(Guid actorUserId, Guid slotId, string idempotencyKey, RescheduleAppointmentCommand command, CancellationToken cancellationToken);
    Task<AppointmentSummary> HoldAsync(Guid actorUserId, Guid slotId, string idempotencyKey, HoldAppointmentCommand command, CancellationToken cancellationToken);
    Task<AppointmentSummary> ConfirmHoldAsync(Guid actorUserId, Guid slotId, string idempotencyKey, HoldAppointmentCommand command, CancellationToken cancellationToken);
    Task<AppointmentSummary> ReleaseHoldAsync(Guid actorUserId, Guid slotId, string idempotencyKey, string? motivo, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AppointmentSummary>> AssignBlockAsync(Guid actorUserId, string idempotencyKey, AssignBlockAppointmentsCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AppointmentSummary>> CancelBlockAsync(Guid actorUserId, string idempotencyKey, CancelBlockAppointmentsCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AppointmentSummary>> CancelTandaAsync(Guid actorUserId, Guid tandaId, string idempotencyKey, string? motivo, CancellationToken cancellationToken);
    Task<AppointmentSummary> ReservePortalAsync(Guid userId, Guid slotId, string? idempotencyKey, CancellationToken cancellationToken);
    Task<AppointmentSummary> CancelPortalAsync(Guid userId, Guid slotId, string? idempotencyKey, string? motivo, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TandaAvailabilitySummary>> GetTandaAvailabilityAsync(DateOnly fechaInicio, DateOnly fechaFin, Guid? patientId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TandaAvailabilityDetailSummary>> GetTandaAvailabilityDetailAsync(DateOnly fechaInicio, DateOnly fechaFin, Guid? patientId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AppointmentSummary>> GetSlotsByTandaAsync(Guid tandaId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AppointmentSummary>> GetActiveSlotsByTandaAsync(Guid tandaId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BlockHistorySummary>> GetBlockHistoryAsync(DateOnly fecha, TimeOnly hora, int? camaraId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BlockHistorySummary>> GetBlockHistoryBySlotAsync(Guid slotId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BlockHistorySummary>> GetBlockHistoryByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, int? camaraId, CancellationToken cancellationToken);
    Task<int> RegisterBlockHistoryAsync(Guid actorUserId, IReadOnlyCollection<BlockHistoryWriteCommand> entries, CancellationToken cancellationToken);
    Task<AppointmentSummary> UpdateOperativeAsync(Guid actorUserId, Guid slotId, AppointmentOperativeCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AppointmentSummary>> UpdateOperativeByTandaAsync(Guid actorUserId, Guid tandaId, AppointmentOperativeCommand command, CancellationToken cancellationToken);
    Task<int> GenerateAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<int> RepairRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken);
}
