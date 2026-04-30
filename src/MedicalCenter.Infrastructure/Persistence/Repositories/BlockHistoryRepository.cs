using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class BlockHistoryRepository(MedicalCenterDbContext dbContext) : IBlockHistoryRepository
{
    public async Task<IReadOnlyCollection<BlockHistory>> GetByBlockAsync(DateOnly fecha, TimeOnly hora, int? camaraId, CancellationToken cancellationToken) =>
        await dbContext.BlockHistories
            .Where(x => x.Fecha == fecha && x.Hora == hora && (!camaraId.HasValue || x.CamaraId == camaraId))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<BlockHistory>> GetBySlotAsync(Guid slotId, CancellationToken cancellationToken) =>
        await dbContext.BlockHistories
            .Where(x => x.SlotId == slotId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<BlockHistory>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, int? camaraId, CancellationToken cancellationToken) =>
        await dbContext.BlockHistories
            .Where(x => x.Fecha >= fechaInicio && x.Fecha <= fechaFin && (!camaraId.HasValue || x.CamaraId == camaraId))
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.Hora)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddRangeAsync(IEnumerable<BlockHistory> entries, CancellationToken cancellationToken) =>
        dbContext.BlockHistories.AddRangeAsync(entries, cancellationToken);

    public void AddRange(IEnumerable<BlockHistory> entries) =>
        dbContext.BlockHistories.AddRange(entries);
}
