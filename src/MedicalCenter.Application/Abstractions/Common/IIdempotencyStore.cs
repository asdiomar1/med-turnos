namespace MedicalCenter.Application.Abstractions.Common;

public interface IIdempotencyStore
{
    Task<IdempotencyReservationResult> ReserveAsync(string operation, string key, string requestHash, CancellationToken cancellationToken);
    Task CompleteAsync(string operation, string key, string responsePayload, CancellationToken cancellationToken);
    Task FailAsync(string operation, string key, CancellationToken cancellationToken);
}
