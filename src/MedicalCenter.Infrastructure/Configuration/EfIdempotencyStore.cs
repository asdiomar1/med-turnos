using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.Infrastructure.Auth;
using MedicalCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Configuration;

public sealed class EfIdempotencyStore(MedicalCenterDbContext dbContext, IClock clock) : IIdempotencyStore
{
    public async Task<IdempotencyReservationResult> ReserveAsync(string operation, string key, string requestHash, CancellationToken cancellationToken)
    {
        var existing = await dbContext.OperationRequests
            .FirstOrDefaultAsync(x => x.Operation == operation && x.Key == key, cancellationToken);

        if (existing is null)
        {
            dbContext.OperationRequests.Add(new OperationRequest(Guid.NewGuid(), operation, key, requestHash, clock.UtcNow));
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return new IdempotencyReservationResult(IdempotencyReservationState.Acquired);
            }
            catch (DbUpdateException)
            {
                existing = await dbContext.OperationRequests
                    .FirstOrDefaultAsync(x => x.Operation == operation && x.Key == key, cancellationToken);
            }
        }

        if (existing is null)
        {
            return new IdempotencyReservationResult(IdempotencyReservationState.Pending);
        }

        if (!existing.HasSamePayload(requestHash))
        {
            return new IdempotencyReservationResult(IdempotencyReservationState.Mismatch);
        }

        if (existing.Status == OperationRequestStatus.Completed)
        {
            return new IdempotencyReservationResult(IdempotencyReservationState.Completed, existing.ResponsePayload);
        }

        if (existing.Status == OperationRequestStatus.Pending)
        {
            return new IdempotencyReservationResult(IdempotencyReservationState.Pending);
        }

        existing.Reopen(requestHash, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new IdempotencyReservationResult(IdempotencyReservationState.Acquired);
    }

    public async Task CompleteAsync(string operation, string key, string responsePayload, CancellationToken cancellationToken)
    {
        var existing = await dbContext.OperationRequests
            .FirstOrDefaultAsync(x => x.Operation == operation && x.Key == key, cancellationToken);

        if (existing is null)
        {
            return;
        }

        existing.MarkCompleted(responsePayload, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task FailAsync(string operation, string key, CancellationToken cancellationToken)
    {
        var existing = await dbContext.OperationRequests
            .FirstOrDefaultAsync(x => x.Operation == operation && x.Key == key, cancellationToken);

        if (existing is null)
        {
            return;
        }

        existing.MarkFailed(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
