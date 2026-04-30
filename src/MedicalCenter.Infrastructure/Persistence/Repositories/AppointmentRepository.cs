using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRepository(
    MedicalCenterDbContext dbContext,
    ILogger<AppointmentRepository> logger) : IAppointmentRepository
{
    public Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken) =>
        dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

    public async Task<IReadOnlyCollection<Appointment>> GetByIdsAsync(IEnumerable<Guid> appointmentIds, CancellationToken cancellationToken)
    {
        var ids = appointmentIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return await dbContext.Appointments
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ThenBy(x => x.CameraId)
            .ThenBy(x => x.Lugar)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryReserveAppointmentAsync(Guid appointmentId, Guid patientId, CancellationToken cancellationToken)
    {
        var appointment = await dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);
        if (appointment is null || !appointment.IsReservable())
        {
            return false;
        }

        appointment.Reserve(patientId);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<bool> TryCommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected database error during TryCommitAsync");
            throw;
        }
    }

    /// <inheritdoc cref="IAppointmentRepository.TryCommitWithPatientLockAsync"/>
    public async Task<bool> TryCommitWithPatientLockAsync(
        Guid patientId,
        DateOnly fecha,
        TimeOnly hora,
        Guid? ignoreSlotId,
        CancellationToken cancellationToken)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Acquire a transaction-scoped advisory lock on the patient so all concurrent
            // assignment attempts for the same patient are serialized. The lock is released
            // automatically when the transaction commits or rolls back.
            await dbContext.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({patientId.ToString("N")}))",
                cancellationToken);

            // Re-verify consecutive-appointment constraint with a fresh DB read so we see
            // any slots committed by concurrent requests that raced past the pre-check.
            var occupiedHoras = await dbContext.Appointments
                .AsNoTracking()
                .Where(x => x.PatientId == patientId
                         && x.Status == AppointmentStatus.Ocupado
                         && x.Fecha == fecha
                         && (!ignoreSlotId.HasValue || x.Id != ignoreSlotId.Value))
                .Select(x => x.Hora)
                .ToListAsync(cancellationToken);

            if (occupiedHoras.Any(h => Math.Abs(ToMinutes(h) - ToMinutes(hora)) <= 60))
            {
                // Clear the change tracker before throwing so the caller's catch
                // (e.g. idempotency FailAsync) can save without the stale staged entities.
                dbContext.ChangeTracker.Clear();
                throw new ConflictException("No puedes reservar turnos consecutivos en el mismo dia");
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return true;
        }
        catch (ConflictException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Unexpected database error during TryCommitWithPatientLockAsync for patient {PatientId}",
                patientId);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private static int ToMinutes(TimeOnly hora) => hora.Hour * 60 + hora.Minute;

    public async Task<IReadOnlyCollection<Appointment>> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken) =>
        await dbContext.Appointments
            .Where(x => x.Fecha == fecha)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ThenBy(x => x.CameraId)
            .ThenBy(x => x.Lugar)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Appointment>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken) =>
        await dbContext.Appointments
            .Where(x => x.Fecha >= fechaInicio && x.Fecha <= fechaFin)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ThenBy(x => x.CameraId)
            .ThenBy(x => x.Lugar)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Appointment>> GetActivosByPacienteAsync(Guid pacienteId, DateOnly fromDate, CancellationToken cancellationToken) =>
        await dbContext.Appointments
            .Where(x => x.PatientId == pacienteId && x.Status == AppointmentStatus.Ocupado && x.Fecha >= fromDate)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ThenBy(x => x.CameraId)
            .ThenBy(x => x.Lugar)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Appointment>> GetOccupiedByPacienteOnDateAsync(Guid pacienteId, DateOnly fecha, CancellationToken cancellationToken) =>
        await dbContext.Appointments
            .AsNoTracking()
            .Where(x => x.PatientId == pacienteId && x.Status == AppointmentStatus.Ocupado && x.Fecha == fecha)
            .OrderBy(x => x.Hora)
            .ThenBy(x => x.CameraId)
            .ThenBy(x => x.Lugar)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Appointment>> GetByBlockAsync(DateOnly fecha, TimeOnly hora, int cameraId, CancellationToken cancellationToken) =>
        await dbContext.Appointments
            .Where(x => x.Fecha == fecha && x.Hora == hora && x.CameraId == cameraId)
            .OrderBy(x => x.Lugar)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Appointment>> GetByTandaIdAsync(Guid tandaId, CancellationToken cancellationToken) =>
        await dbContext.Appointments
            .Where(x => x.TandaId == tandaId)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ThenBy(x => x.CameraId)
            .ThenBy(x => x.Lugar)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Appointment appointment, CancellationToken cancellationToken) =>
        dbContext.Appointments.AddAsync(appointment, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<Appointment>> GetFutureExcessByCameraAsync(int cameraId, DateOnly from, int minLugar, CancellationToken cancellationToken) =>
        await dbContext.Appointments
            .Where(x => x.CameraId == cameraId && x.Fecha >= from && x.Lugar >= minLugar)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ThenBy(x => x.Lugar)
            .ToListAsync(cancellationToken);

    public Task DeleteRangeAsync(IEnumerable<Appointment> appointments, CancellationToken cancellationToken)
    {
        dbContext.Appointments.RemoveRange(appointments);
        return Task.CompletedTask;
    }
}
