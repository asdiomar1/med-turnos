using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IScheduleHourRepository
{
    Task<IReadOnlyCollection<ScheduleHour>> GetAsync(CancellationToken cancellationToken);
    Task<ScheduleHour?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> GetNextIdAsync(CancellationToken cancellationToken);
    Task AddAsync(ScheduleHour scheduleHour, CancellationToken cancellationToken);
}
