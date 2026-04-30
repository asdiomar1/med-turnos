using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class OutOfHoursTurnRepository(MedicalCenterDbContext dbContext) : IOutOfHoursTurnRepository
{
    public async Task<IReadOnlyCollection<OutOfHoursTurn>> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken) =>
        await dbContext.OutOfHoursTurns
            .Where(x => x.Fecha == fecha)
            .OrderBy(x => x.Hora)
            .ToListAsync(cancellationToken);

    public Task<OutOfHoursTurn?> GetByIdAsync(Guid turnoId, CancellationToken cancellationToken) =>
        dbContext.OutOfHoursTurns.FirstOrDefaultAsync(x => x.Id == turnoId, cancellationToken);

    public Task AddAsync(OutOfHoursTurn turno, CancellationToken cancellationToken) =>
        dbContext.OutOfHoursTurns.AddAsync(turno, cancellationToken).AsTask();

    public Task DeleteAsync(OutOfHoursTurn turno, CancellationToken cancellationToken)
    {
        dbContext.OutOfHoursTurns.Remove(turno);
        return Task.CompletedTask;
    }
}
