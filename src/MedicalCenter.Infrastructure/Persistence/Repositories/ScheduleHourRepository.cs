using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class ScheduleHourRepository(MedicalCenterDbContext dbContext) : IScheduleHourRepository
{
    public async Task<IReadOnlyCollection<ScheduleHour>> GetAsync(CancellationToken cancellationToken) =>
        await dbContext.ScheduleHours.OrderBy(x => x.Orden).ToListAsync(cancellationToken);

    public Task<ScheduleHour?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.ScheduleHours.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken) =>
        (await dbContext.ScheduleHours.MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0) + 1;

    public Task AddAsync(ScheduleHour scheduleHour, CancellationToken cancellationToken) =>
        dbContext.ScheduleHours.AddAsync(scheduleHour, cancellationToken).AsTask();
}
