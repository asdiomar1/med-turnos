namespace MedicalCenter.Application.Abstractions.Common;

public sealed record IdempotencyReservationResult(IdempotencyReservationState State, string? ResponsePayload = null);

public enum IdempotencyReservationState
{
    Acquired = 0,
    Completed = 1,
    Pending = 2,
    Mismatch = 3
}
