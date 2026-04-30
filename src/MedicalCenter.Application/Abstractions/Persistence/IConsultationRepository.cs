using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IConsultationRepository
{
    Task<IReadOnlyCollection<ConsultationScheduleHour>> GetScheduleHoursAsync(CancellationToken cancellationToken);
    Task<ConsultationScheduleHour?> GetScheduleHourByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> GetNextScheduleHourIdAsync(CancellationToken cancellationToken);
    Task AddScheduleHourAsync(ConsultationScheduleHour scheduleHour, CancellationToken cancellationToken);
    Task AddAsync(ConsultationSlot slot, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ConsultationSlot>> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ConsultationSlot>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken);
    Task<ConsultationSlot?> GetByIdAsync(Guid slotId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ConsultationSession>> GetSessionsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken);
    Task<int> CountFutureSlotsByHourAsync(TimeOnly hora, DateOnly fromDate, CancellationToken cancellationToken);
}
