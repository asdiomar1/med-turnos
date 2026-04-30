using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IOutOfHoursTurnRepository
{
    Task<IReadOnlyCollection<OutOfHoursTurn>> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<OutOfHoursTurn?> GetByIdAsync(Guid turnoId, CancellationToken cancellationToken);
    Task AddAsync(OutOfHoursTurn turno, CancellationToken cancellationToken);
    Task DeleteAsync(OutOfHoursTurn turno, CancellationToken cancellationToken);
}
