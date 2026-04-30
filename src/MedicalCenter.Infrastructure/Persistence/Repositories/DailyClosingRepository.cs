using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class DailyClosingRepository(MedicalCenterDbContext dbContext) : IDailyClosingRepository
{
    public Task<DailyClosing?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.DailyClosings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<DailyClosing?> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken) =>
        dbContext.DailyClosings.FirstOrDefaultAsync(x => x.Fecha == fecha, cancellationToken);

    public async Task<IReadOnlyCollection<DailyClosing>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken) =>
        await dbContext.DailyClosings
            .Where(x => x.Fecha.Year == year && x.Fecha.Month == month)
            .OrderBy(x => x.Fecha)
            .ToListAsync(cancellationToken);

    public Task AddAsync(DailyClosing closing, CancellationToken cancellationToken) =>
        dbContext.DailyClosings.AddAsync(closing, cancellationToken).AsTask();
}
