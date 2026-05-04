using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Appointment>> GetByIdsAsync(IEnumerable<Guid> appointmentIds, CancellationToken cancellationToken);
    Task<bool> TryReserveAppointmentAsync(Guid appointmentId, Guid patientId, CancellationToken cancellationToken);
    Task<bool> TryCommitAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Appointment>> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Appointment>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, int? offset, int? limit, CancellationToken cancellationToken);
    Task<int> CountByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BlockHistory>> GetBlockHistoryByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Appointment>> GetActivosByPacienteAsync(Guid pacienteId, DateOnly fromDate, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Appointment>> GetOccupiedByPacienteOnDateAsync(Guid pacienteId, DateOnly fecha, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Appointment>> GetByBlockAsync(DateOnly fecha, TimeOnly hora, int cameraId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Appointment>> GetByTandaIdAsync(Guid tandaId, CancellationToken cancellationToken);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Appointment>> GetFutureExcessByCameraAsync(int cameraId, DateOnly from, int minLugar, CancellationToken cancellationToken);
    Task DeleteRangeAsync(IEnumerable<Appointment> appointments, CancellationToken cancellationToken);

    /// <summary>
    /// Saves all tracked changes inside an explicit transaction protected by a PostgreSQL
    /// transaction-scoped advisory lock keyed on <paramref name="patientId"/>.
    /// Inside the lock the consecutive-appointment constraint is re-verified against the
    /// database so concurrent assignment requests for the same patient are serialized.
    /// Returns <c>false</c> on optimistic concurrency conflict; throws
    /// <see cref="MedicalCenter.Application.Exceptions.ConflictException"/> when the
    /// consecutive-appointment check fails; re-throws any other unexpected exception
    /// after clearing the change tracker.
    /// </summary>
    Task<bool> TryCommitWithPatientLockAsync(
        Guid patientId,
        DateOnly fecha,
        TimeOnly hora,
        Guid? ignoreSlotId,
        CancellationToken cancellationToken);
}
