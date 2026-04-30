using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IScheduleRepository
{
    Task AddAsync(Schedule schedule, CancellationToken cancellationToken);
    Task DeleteRangeByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
}
