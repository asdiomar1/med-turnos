using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.OutOfHoursTurns;

public interface IOutOfHoursTurnsService
{
    Task<IReadOnlyCollection<OutOfHoursTurnSummary>> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<OutOfHoursTurnSummary> CreateAsync(Guid actorUserId, OutOfHoursTurnCreateCommand command, string idempotencyKey, CancellationToken cancellationToken);
    Task<OutOfHoursTurnSummary> CancelAsync(Guid actorUserId, Guid turnoId, string idempotencyKey, CancellationToken cancellationToken);
}
