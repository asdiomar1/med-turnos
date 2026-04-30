using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IDailyClosingRepository
{
    Task<DailyClosing?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DailyClosing?> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DailyClosing>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken);
    Task AddAsync(DailyClosing closing, CancellationToken cancellationToken);
}
