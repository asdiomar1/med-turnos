using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IBlockHistoryRepository
{
    Task<IReadOnlyCollection<BlockHistory>> GetByBlockAsync(DateOnly fecha, TimeOnly hora, int? camaraId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BlockHistory>> GetBySlotAsync(Guid slotId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BlockHistory>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, int? camaraId, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<BlockHistory> entries, CancellationToken cancellationToken);
    void AddRange(IEnumerable<BlockHistory> entries);
}
