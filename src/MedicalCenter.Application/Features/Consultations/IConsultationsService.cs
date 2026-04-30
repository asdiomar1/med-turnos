using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Consultations;

public interface IConsultationsService
{
    Task<IReadOnlyCollection<ConsultationScheduleHourSummary>> GetScheduleHoursAsync(CancellationToken cancellationToken);
    Task<ConsultationScheduleHourSummary> CreateScheduleHourAsync(ConsultationScheduleHourUpsertCommand command, CancellationToken cancellationToken);
    Task<ConsultationScheduleHourSummary> UpdateScheduleHourAsync(int id, ConsultationScheduleHourUpsertCommand command, CancellationToken cancellationToken);
    Task<ConsultationScheduleHourSummary> ToggleScheduleHourAsync(int id, bool activo, CancellationToken cancellationToken);
    Task<ConsultationScheduleHourDeletionPreviewSummary> PreviewDeleteScheduleHourAsync(int id, CancellationToken cancellationToken);
    Task<ConsultationScheduleHourSummary?> DeleteScheduleHourAsync(int id, CancellationToken cancellationToken);
    Task<int> GenerateAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<int> RepairRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ConsultationSlotSummary>> GetByDateAsync(DateOnly? fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ConsultationSlotSummary>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken);
    Task<ConsultationSlotSummary> AssignAsync(Guid actorUserId, Guid slotId, string idempotencyKey, AssignConsultationCommand command, CancellationToken cancellationToken);
    Task<ConsultationSlotSummary> CancelAsync(Guid actorUserId, Guid slotId, string idempotencyKey, CancelConsultationCommand command, CancellationToken cancellationToken);
    Task<ConsultationSlotSummary> RescheduleAsync(Guid actorUserId, Guid slotId, string idempotencyKey, RescheduleConsultationCommand command, CancellationToken cancellationToken);
    Task<ConsultationSlotSummary> CloseAsync(Guid actorUserId, Guid slotId, string idempotencyKey, CloseConsultationCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ConsultationSessionSummary>> GetCompletedSessionsAsync(Guid patientId, CancellationToken cancellationToken);
}
