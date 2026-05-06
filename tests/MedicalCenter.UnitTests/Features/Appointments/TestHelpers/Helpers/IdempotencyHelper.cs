using System.Text.Json;
using MedicalCenter.Application.Abstractions.Common;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Helpers;

/// <summary>
/// Helper methods for setting up IIdempotencyStore mock states in tests.
/// </summary>
public static class IdempotencyHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Sets up the idempotency store to return Acquired state (new operation).
    /// </summary>
    public static void SetupAcquired(this IIdempotencyStore store, string operation, string key)
    {
        store.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));
    }

    /// <summary>
    /// Sets up the idempotency store to return Completed state with a cached result.
    /// </summary>
    public static void SetupCompleted<T>(this IIdempotencyStore store, string operation, string key, T response)
    {
        var payload = JsonSerializer.Serialize(response, SerializerOptions);
        store.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Completed, payload));
    }

    /// <summary>
    /// Sets up the idempotency store to return Completed state with a raw JSON payload.
    /// </summary>
    public static void SetupCompleted(this IIdempotencyStore store, string operation, string key, string jsonPayload)
    {
        store.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Completed, jsonPayload));
    }

    /// <summary>
    /// Sets up the idempotency store to return Pending state (operation in progress).
    /// </summary>
    public static void SetupPending(this IIdempotencyStore store, string operation, string key)
    {
        store.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Pending));
    }

    /// <summary>
    /// Sets up the idempotency store to return Mismatch state (different payload).
    /// </summary>
    public static void SetupMismatch(this IIdempotencyStore store, string operation, string key)
    {
        store.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Mismatch));
    }

    /// <summary>
    /// Sets up the idempotency store with a specific state.
    /// </summary>
    public static void SetupState(this IIdempotencyStore store, string operation, string key, IdempotencyReservationState state, object? response = null)
    {
        string? payload = null;
        if (response is not null && state == IdempotencyReservationState.Completed)
        {
            payload = JsonSerializer.Serialize(response, SerializerOptions);
        }

        store.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(state, payload));
    }

    /// <summary>
    /// Verifies that CompleteAsync was called exactly once for the operation.
    /// </summary>
    public static async Task ShouldCompleteIdempotencyAsync(this IIdempotencyStore store, string operation, string key)
    {
        await store.Received(1).CompleteAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that FailAsync was called exactly once for the operation.
    /// </summary>
    public static async Task ShouldFailIdempotencyAsync(this IIdempotencyStore store, string operation, string key)
    {
        await store.Received(1).FailAsync(operation, key, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that neither CompleteAsync nor FailAsync were called for the operation.
    /// </summary>
    public static void ShouldNotModifyIdempotency(this IIdempotencyStore store, string operation, string key)
    {
        store.DidNotReceive().CompleteAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>());
        store.DidNotReceive().FailAsync(operation, key, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that ReserveAsync was called exactly once for the operation.
    /// </summary>
    public static void ShouldReserveIdempotency(this IIdempotencyStore store, string operation, string key)
    {
        store.Received(1).ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
