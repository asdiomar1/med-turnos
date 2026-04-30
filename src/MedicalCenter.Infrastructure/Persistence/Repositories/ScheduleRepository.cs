using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class ScheduleRepository(MedicalCenterDbContext dbContext) : IScheduleRepository
{
    public Task AddAsync(Schedule schedule, CancellationToken cancellationToken) =>
        dbContext.Schedules.AddAsync(schedule, cancellationToken).AsTask();

    public async Task DeleteRangeByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return;
        await dbContext.Schedules
            .Where(x => idList.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
