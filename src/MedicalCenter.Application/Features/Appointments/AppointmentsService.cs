using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.WhatsApp;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Constants;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Application.Features.Appointments;

public sealed class AppointmentsService : IAppointmentsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const string PatientNotFoundMessage = "Paciente no encontrado.";
    private const string PatientNotActiveMessage = "El paciente no se encuentra activo.";
    private const string AppointmentNotFoundMessage = "Turno no encontrado.";
    private const string ForbiddenMessage = "Prohibido";
    private readonly IAppointmentRepository appointmentRepository;
    private readonly IScheduleRepository scheduleRepository;
    private readonly IUserRepository userRepository;
    private readonly IScheduleHourRepository scheduleHourRepository;
    private readonly ICameraRepository cameraRepository;
    private readonly IPatientRepository patientRepository;
    private readonly IMedicoRepository medicoRepository;
    private readonly IReferenteRepository referenteRepository;
    private readonly IObraSocialRepository obraSocialRepository;
    private readonly IBlockHistoryRepository blockHistoryRepository;
    private readonly IWhatsappService whatsappService;
    private readonly IUnitOfWork unitOfWork;
    private readonly IIdempotencyStore idempotencyStore;
    private readonly IClock clock;

    public AppointmentsService(AppointmentsDataAccessDependencies dataAccess, AppointmentsRuntimeDependencies runtime)
    {
        appointmentRepository = dataAccess.AppointmentRepository;
        scheduleRepository = dataAccess.ScheduleRepository;
        userRepository = dataAccess.UserRepository;
        scheduleHourRepository = dataAccess.ScheduleHourRepository;
        cameraRepository = dataAccess.CameraRepository;
        patientRepository = dataAccess.PatientRepository;
        medicoRepository = dataAccess.MedicoRepository;
        referenteRepository = dataAccess.ReferenteRepository;
        obraSocialRepository = dataAccess.ObraSocialRepository;
        blockHistoryRepository = dataAccess.BlockHistoryRepository;
        whatsappService = runtime.WhatsappService;
        unitOfWork = runtime.UnitOfWork;
        idempotencyStore = runtime.IdempotencyStore;
        clock = runtime.Clock;
    }

    public async Task<IReadOnlyCollection<AppointmentSummary>> GetByDateAsync(DateOnly? fecha, CancellationToken cancellationToken)
    {
        if (fecha is null)
        {
            return [];
        }

        var appointments = await appointmentRepository.GetByDateAsync(fecha.Value, cancellationToken);
        return await FilterAndMapAsync(appointments, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AppointmentSummary>> GetDisponiblesPortalByDateAsync(Guid userId, DateOnly fecha, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();
        var patientId = await ResolvePortalPatientIdAsync(user, cancellationToken);
        var appointments = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);

        var filtered = appointments
            .Where(x =>
                (patientId.HasValue && x.PatientId == patientId.Value) ||
                (x.PatientId is null && x.Status is AppointmentStatus.Libre or AppointmentStatus.Cancelado))
            .ToArray();
        return await FilterAndMapAsync(filtered, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AppointmentGroupSummary>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByRangeAsync(fechaInicio, fechaFin, null, null, cancellationToken);
        var activeHours = await GetActiveHoursAsync(cancellationToken);
        return appointments
            .Where(x => activeHours.Contains(x.Hora.ToString("HH:mm")))
            .GroupBy(x => x.Fecha)
            .OrderBy(x => x.Key)
            .Select(group => new AppointmentGroupSummary(group.Key, group.Select(x => x.ToSummary()).ToArray()))
            .ToArray();
    }

    public async Task<EnrichedPagedResult> GetEnrichedByRangeAsync(
        DateOnly fechaInicio, DateOnly fechaFin, int offset, int limit, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByRangeAsync(fechaInicio, fechaFin, offset, limit, cancellationToken);
        if (appointments.Count == 0)
        {
            return new EnrichedPagedResult([], 0);
        }

        var activeHours = await GetActiveHoursAsync(cancellationToken);
        appointments = FilterAppointmentsByActiveHours(appointments, activeHours);

        // NOTE: these repositories share the same scoped DbContext, so they must run sequentially.
        var total = await appointmentRepository.CountByRangeAsync(fechaInicio, fechaFin, cancellationToken);
        var lookupIds = CollectLookupIds(appointments);
        var lookups = await LoadTurnoEnrichmentLookupsAsync(lookupIds, fechaInicio, fechaFin, cancellationToken);
        var results = MapTurnoEnrichedSummaries(appointments, lookups);

        return new EnrichedPagedResult(results, total);
    }

    public async Task<IReadOnlyCollection<TurnoEnrichedSummary>> GetEnrichedByDateAsync(
        DateOnly fecha, CancellationToken cancellationToken)
    {
        var result = await GetEnrichedByRangeAsync(fecha, fecha, 0, int.MaxValue, cancellationToken);
        return result.Items;
    }

    public async Task<IReadOnlyCollection<AppointmentSummary>> GetActivosByPacienteAsync(Guid pacienteId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.UtcNow.UtcDateTime, GetArgentinaTimeZone()).Date);
        var appointments = await appointmentRepository.GetActivosByPacienteAsync(pacienteId, today, cancellationToken);
        return await FilterAndMapAsync(appointments, cancellationToken);
    }

    public Task<AppointmentSummary> AssignAsync(Guid actorUserId, Guid slotId, string idempotencyKey, AssignAppointmentCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);

        return ExecuteIdempotentAsync(
            $"turnos.asignaciones:{slotId}",
            idempotencyKey,
            command,
            async () =>
            {
                var patient = await patientRepository.GetByIdAsync(command.PacienteId, cancellationToken)
                    ?? throw new NotFoundException(PatientNotFoundMessage);

                if (!patient.IsActive)
                    throw new ConflictException(PatientNotActiveMessage);

                await ValidateMedicoUserAsync(command.MedicoUserId, cancellationToken);

                var appointment = await appointmentRepository.GetByIdAsync(slotId, cancellationToken)
                    ?? throw new NotFoundException(AppointmentNotFoundMessage);

                EnsureNotPast(appointment);

                if (!appointment.IsReservable())
                    throw new ConflictException("El turno ya no está disponible.");

                // Fast-fail pre-check (no lock yet — avoids the advisory lock overhead in the
                // common case where a conflict is obvious from the current DB state).
                await EnsurePatientHasNoConsecutiveAppointmentsAsync(command.PacienteId, appointment.Fecha, appointment.Hora, null, cancellationToken);

                // EsTanda=false → tandaId siempre null (regla de dominio: un slot no-tanda no tiene tanda).
                // EsTanda=true y TandaId=null → se deriva un Guid determinístico del idempotencyKey
                // para garantizar el mismo valor en cada intento con la misma clave.
                Guid? tandaId = command.EsTanda
                    ? command.TandaId ?? DeterministicGuid(idempotencyKey, "tanda")
                    : null;

                appointment.Reserve(command.PacienteId, command.Accion, command.EsTanda, tandaId, BuildOperativeData(command));

                // Historial y WhatsApp se agregan al mismo DbContext ANTES del commit para que
                // los tres cambios persistan en la misma transacción de base de datos.
                StageHistory(appointment, "asignado", actorUserId, null, appointment.PatientId, appointment.TandaId);
                await whatsappService.EnqueueTurnoConfirmadoAsync(appointment, "turnos_asignacion", cancellationToken);

                // TryCommitWithPatientLockAsync acquires a PG advisory lock on the patient,
                // re-verifies the consecutive-appointment constraint inside the lock (to catch
                // races that slipped past the pre-check above) and commits atomically.
                if (!await appointmentRepository.TryCommitWithPatientLockAsync(
                        command.PacienteId, appointment.Fecha, appointment.Hora, null, cancellationToken))
                    throw new ConflictException("El turno ya no está disponible.");

                return appointment.ToSummary();
            },
            cancellationToken);
    }

    public Task<AppointmentSummary> CancelAsync(Guid actorUserId, Guid slotId, string idempotencyKey, string? motivo, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);

        return ExecuteIdempotentAsync(
            $"turnos.cancelaciones:{slotId}",
            idempotencyKey,
            new { slotId, motivo },
            async () =>
            {
                var appointment = await appointmentRepository.GetByIdAsync(slotId, cancellationToken)
                    ?? throw new NotFoundException(AppointmentNotFoundMessage);

                EnsureNotPast(appointment);
                var cancelPatientId = appointment.PatientId;
                var cancelTandaId = appointment.TandaId;

                try
                {
                    // Stage WhatsApp and history BEFORE cancel so the appointment still carries
                    // its operative data; all three writes land in the same TryCommitAsync transaction.
                    await whatsappService.EnqueueTurnoCancelacionAsync(appointment, "turnos_cancelacion", idempotencyKey, cancellationToken);
                    StageHistory(appointment, "cancelado", actorUserId, motivo, cancelPatientId, cancelTandaId);
                    appointment.Cancel(motivo);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ConflictException(exception.Message);
                }

                if (!await appointmentRepository.TryCommitAsync(cancellationToken))
                {
                    throw new ConflictException("No se pudo cancelar el turno por concurrencia.");
                }

                return appointment.ToSummary();
            },
            cancellationToken);
    }

    public Task<AppointmentSummary> RescheduleAsync(Guid actorUserId, Guid slotId, string idempotencyKey, RescheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);

        return ExecuteIdempotentAsync(
            $"turnos.reprogramaciones:{slotId}",
            idempotencyKey,
            command,
            async () =>
            {
                if (slotId == command.TargetSlotId)
                {
                    throw new ValidationException("El slot destino debe ser distinto del slot origen.");
                }

                var source = await appointmentRepository.GetByIdAsync(slotId, cancellationToken)
                    ?? throw new NotFoundException("Turno origen no encontrado.");
                var target = await appointmentRepository.GetByIdAsync(command.TargetSlotId, cancellationToken)
                    ?? throw new NotFoundException("Turno destino no encontrado.");

                EnsureNotPast(source);
                EnsureNotPast(target);

                if (string.Equals(command.Scope, "normal", StringComparison.OrdinalIgnoreCase))
                {
                    return await RescheduleSingleAsync(source, target, actorUserId, cancellationToken);
                }

                if (string.Equals(command.Scope, "tanda", StringComparison.OrdinalIgnoreCase) || string.Equals(command.Scope, "bloque_tanda", StringComparison.OrdinalIgnoreCase))
                {
                    return await RescheduleGroupedAsync(source, target, actorUserId, cancellationToken);
                }

                throw new ValidationException("scope invalido");
            },
            cancellationToken);
    }

    public Task<AppointmentSummary> HoldAsync(Guid actorUserId, Guid slotId, string idempotencyKey, HoldAppointmentCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);

        return ExecuteIdempotentAsync(
            $"turnos.apartados:{slotId}",
            idempotencyKey,
            new { slotId, actorUserId, command.PacienteId, command.EsMonoxido, command.ReferidoTercero, command.ReferenteId, command.ModalidadCobro, command.ObraSocialId, command.NumeroAutorizacion, command.SesionesAutorizadas, command.CicloObraSocialId, command.IniciarNuevoCicloObraSocial, command.ConvenioCorroborado, command.MedicoId, command.EsNuevoIngreso, command.MonoxidoOrdenMedica, command.MonoxidoResumenClinico },
            async () =>
            {
                if (command.PacienteId.HasValue)
                {
                    var patient = await patientRepository.GetByIdAsync(command.PacienteId.Value, cancellationToken)
                        ?? throw new NotFoundException(PatientNotFoundMessage);

                    if (!patient.IsActive)
                    {
                        throw new ConflictException(PatientNotActiveMessage);
                    }
                }

                var appointment = await appointmentRepository.GetByIdAsync(slotId, cancellationToken)
                    ?? throw new NotFoundException(AppointmentNotFoundMessage);

                EnsureNotPast(appointment);

                try
                {
                    appointment.Hold(command.PacienteId, actorUserId, clock.UtcNow, null, command.EsMonoxido, null, BuildOperativeData(command));
                    StageHistory(appointment, "apartado", actorUserId, null, appointment.PatientId, appointment.TandaId);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ConflictException(exception.Message);
                }

                if (!await appointmentRepository.TryCommitAsync(cancellationToken))
                {
                    throw new ConflictException("El turno ya no esta disponible.");
                }

                return appointment.ToSummary();
            },
            cancellationToken);
    }

    public Task<AppointmentSummary> ConfirmHoldAsync(Guid actorUserId, Guid slotId, string idempotencyKey, HoldAppointmentCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);

        return ExecuteIdempotentAsync(
            $"turnos.apartados.confirmaciones:{slotId}",
            idempotencyKey,
            new { slotId, command.PacienteId, command.EsMonoxido, command.ReferidoTercero, command.ReferenteId, command.ModalidadCobro, command.ObraSocialId, command.NumeroAutorizacion, command.SesionesAutorizadas, command.CicloObraSocialId, command.IniciarNuevoCicloObraSocial, command.ConvenioCorroborado, command.MedicoId, command.EsNuevoIngreso, command.MonoxidoOrdenMedica, command.MonoxidoResumenClinico },
            async () =>
            {
                if (command.PacienteId.HasValue)
                {
                    var patient = await patientRepository.GetByIdAsync(command.PacienteId.Value, cancellationToken)
                        ?? throw new NotFoundException(PatientNotFoundMessage);

                    if (!patient.IsActive)
                    {
                        throw new ConflictException(PatientNotActiveMessage);
                    }
                }

                var appointment = await appointmentRepository.GetByIdAsync(slotId, cancellationToken)
                    ?? throw new NotFoundException(AppointmentNotFoundMessage);

                EnsureNotPast(appointment);

                var finalPatientId = command.PacienteId ?? appointment.PatientId;
                if (!finalPatientId.HasValue)
                {
                    throw new ConflictException("Paciente requerido para confirmar el apartado.");
                }

                // Fast-fail pre-check; the advisory lock re-verifies inside TryCommitWithPatientLockAsync.
                await EnsurePatientHasNoConsecutiveAppointmentsAsync(finalPatientId.Value, appointment.Fecha, appointment.Hora, appointment.Id, cancellationToken);

                try
                {
                    appointment.ConfirmHold(command.PacienteId, null, BuildOperativeData(command));
                    StageHistory(appointment, "apartado_confirmado", actorUserId, null, appointment.PatientId, appointment.TandaId);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ConflictException(exception.Message);
                }

                await whatsappService.EnqueueTurnoConfirmadoAsync(appointment, "turnos_confirmacion_apartado", cancellationToken);

                if (!await appointmentRepository.TryCommitWithPatientLockAsync(
                        finalPatientId.Value, appointment.Fecha, appointment.Hora, appointment.Id, cancellationToken))
                {
                    throw new ConflictException("No se pudo confirmar el apartado por concurrencia.");
                }

                return appointment.ToSummary();
            },
            cancellationToken);
    }

    public Task<AppointmentSummary> ReleaseHoldAsync(Guid actorUserId, Guid slotId, string idempotencyKey, string? motivo, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);

        return ExecuteIdempotentAsync(
            $"turnos.apartados.liberaciones:{slotId}",
            idempotencyKey,
            new { slotId, motivo },
            async () =>
            {
                var appointment = await appointmentRepository.GetByIdAsync(slotId, cancellationToken)
                    ?? throw new NotFoundException(AppointmentNotFoundMessage);

                EnsureNotPast(appointment);
                var holdPatientId = appointment.PatientId;

                try
                {
                    // Stage before ReleaseHold so operative data is still available for the history entry.
                    StageHistory(appointment, "apartado_liberado", actorUserId, motivo, holdPatientId, appointment.TandaId);
                    appointment.ReleaseHold(motivo);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ConflictException(exception.Message);
                }

                if (!await appointmentRepository.TryCommitAsync(cancellationToken))
                {
                    throw new ConflictException("No se pudo liberar el apartado por concurrencia.");
                }

                return appointment.ToSummary();
            },
            cancellationToken);
    }

    public Task<IReadOnlyCollection<AppointmentSummary>> AssignBlockAsync(Guid actorUserId, string idempotencyKey, AssignBlockAppointmentsCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);

        return ExecuteIdempotentCollectionAsync(
            $"turnos.bloques.asignaciones:{command.Fecha}:{command.Hora}:{command.CamaraId}",
            idempotencyKey,
            command,
            async () =>
            {
                var patient = await patientRepository.GetByIdAsync(command.PacienteId, cancellationToken)
                    ?? throw new NotFoundException(PatientNotFoundMessage);

                if (!patient.IsActive)
                {
                    throw new ConflictException(PatientNotActiveMessage);
                }

                var slots = (await appointmentRepository.GetByBlockAsync(command.Fecha, command.Hora, command.CamaraId, cancellationToken))
                    .OrderBy(x => x.Lugar)
                    .ToArray();

                if (slots.Length == 0)
                {
                    throw new NotFoundException("No se encontraron slots para el bloque solicitado.");
                }

                EnsureNotPast(slots[0]);

                if (slots.Any(x => !x.IsReservable()))
                {
                    throw new ConflictException("El bloque ya no esta completamente disponible.");
                }

                await EnsurePatientHasNoConsecutiveAppointmentsAsync(command.PacienteId, command.Fecha, command.Hora, null, cancellationToken);

                var blockId = Guid.NewGuid();
                var tandaId = command.EsTanda ? command.TandaId ?? Guid.NewGuid() : command.TandaId;
                var operative = BuildOperativeData(command);

                foreach (var slot in slots)
                {
                    slot.Reserve(command.PacienteId, "bloque_completo", command.EsTanda, tandaId, operative);
                    slot.AssignBlock(blockId, command.EsTanda, tandaId, operative);
                    slot.AssignTanda(tandaId);
                    await whatsappService.EnqueueTurnoConfirmadoAsync(slot, "turnos_asignacion_bloque", cancellationToken);
                }

                // Stage the single summary history entry before commit so it lands in the same transaction.
                StageBlockHistory(slots, slots[0].PatientId, slots[0].TandaId, "bloque_asignado", actorUserId, null);

                if (!await appointmentRepository.TryCommitAsync(cancellationToken))
                {
                    throw new ConflictException("No se pudo asignar el bloque por concurrencia.");
                }

                return slots.Select(x => x.ToSummary()).ToArray();
            },
            cancellationToken);
    }

    public Task<IReadOnlyCollection<AppointmentSummary>> CancelBlockAsync(Guid actorUserId, string idempotencyKey, CancelBlockAppointmentsCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);

        return ExecuteIdempotentCollectionAsync(
            $"turnos.bloques.cancelaciones:{command.Fecha}:{command.Hora}:{command.CamaraId}",
            idempotencyKey,
            command,
            async () =>
            {
                var slots = (await appointmentRepository.GetByBlockAsync(command.Fecha, command.Hora, command.CamaraId, cancellationToken))
                    .OrderBy(x => x.Lugar)
                    .ToArray();

                if (slots.Length == 0)
                {
                    throw new NotFoundException("No se encontraron slots para el bloque solicitado.");
                }

                EnsureNotPast(slots[0]);

                var targetedSlots = slots
                    .Where(x => x.PatientId == command.PacienteId && x.Status == AppointmentStatus.Ocupado && x.EsBloqueCompleto)
                    .ToArray();

                if (targetedSlots.Length == 0)
                {
                    throw new NotFoundException("No se encontraron slots para el paciente indicado.");
                }

                var cancelSnapshots = targetedSlots.Select(s => (s, s.PatientId, s.TandaId)).ToArray();

                await whatsappService.EnqueueTurnosCancelacionAsync(command.PacienteId, targetedSlots, idempotencyKey, "turnos_cancelacion_bloque", cancellationToken);

                // Stage history BEFORE cancel so the first slot still carries its operative data.
                StageBlockHistory(targetedSlots, cancelSnapshots[0].PatientId, cancelSnapshots[0].TandaId, "bloque_cancelado", actorUserId, command.Motivo);

                foreach (var slot in targetedSlots)
                {
                    if (slot.Status == AppointmentStatus.Apartado)
                    {
                        slot.ReleaseHold(command.Motivo);
                    }
                    else
                    {
                        slot.Cancel(command.Motivo);
                    }
                }

                if (!await appointmentRepository.TryCommitAsync(cancellationToken))
                {
                    throw new ConflictException("No se pudo cancelar el bloque por concurrencia.");
                }

                return slots.Select(x => x.ToSummary()).ToArray();
            },
            cancellationToken);
    }

    public Task<IReadOnlyCollection<AppointmentSummary>> CancelTandaAsync(Guid actorUserId, Guid tandaId, string idempotencyKey, string? motivo, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);

        return ExecuteIdempotentCollectionAsync(
            $"turnos.tandas.cancelaciones:{tandaId}",
            idempotencyKey,
            new { tandaId, motivo },
            async () =>
            {
                var slots = (await appointmentRepository.GetByTandaIdAsync(tandaId, cancellationToken))
                    .OrderBy(x => x.Fecha)
                    .ThenBy(x => x.Hora)
                    .ThenBy(x => x.Lugar)
                    .ToArray();

                if (slots.Length == 0)
                {
                    throw new NotFoundException("No se encontraron slots para la tanda solicitada.");
                }

                var slotsToCancel = slots.Where(x => x.Status is AppointmentStatus.Ocupado or AppointmentStatus.Apartado).ToArray();
                if (slotsToCancel.Length == 0)
                {
                    throw new NotFoundException("No se encontraron slots ocupados para la tanda solicitada.");
                }

                var patientId = slotsToCancel.Select(x => x.PatientId).FirstOrDefault(x => x.HasValue);
                if (!patientId.HasValue)
                {
                    throw new ConflictException("Paciente no encontrado para la tanda solicitada.");
                }

                var cancelSnapshots = slotsToCancel.Select(s => (s, s.PatientId, s.TandaId)).ToArray();

                await whatsappService.EnqueueTurnosCancelacionAsync(patientId.Value, slotsToCancel, idempotencyKey, "turnos_cancelacion_tanda", cancellationToken);

                // Stage all history entries BEFORE cancel so slots still carry their operative data.
                StageHistoryRange(cancelSnapshots, "tanda_cancelada", actorUserId, motivo);

                foreach (var slot in slotsToCancel)
                {
                    EnsureNotPast(slot);
                    if (slot.Status == AppointmentStatus.Apartado)
                    {
                        slot.ReleaseHold(motivo);
                    }
                    else
                    {
                        slot.Cancel(motivo);
                    }
                }

                if (!await appointmentRepository.TryCommitAsync(cancellationToken))
                {
                    throw new ConflictException("No se pudo cancelar la tanda por concurrencia.");
                }

                return slots.Select(x => x.ToSummary()).ToArray();
            },
            cancellationToken);
    }

    public async Task<AppointmentSummary> ReservePortalAsync(Guid userId, Guid slotId, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();
        var patientId = await ResolvePortalPatientIdAsync(user, cancellationToken);
        if (!patientId.HasValue)
        {
            throw new ForbiddenException(ForbiddenMessage);
        }

        return await ExecuteOptionalIdempotentAsync(
            $"portal.turnos.reservas:{slotId}",
            idempotencyKey,
            new { userId, slotId },
            async () =>
            {
                var appointment = await appointmentRepository.GetByIdAsync(slotId, cancellationToken)
                    ?? throw new NotFoundException(AppointmentNotFoundMessage);

                EnsureNotPast(appointment);
                await EnsurePatientHasNoConsecutiveAppointmentsAsync(patientId.Value, appointment.Fecha, appointment.Hora, null, cancellationToken);

                try
                {
                    appointment.Reserve(patientId.Value);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ConflictException(exception.Message);
                }

                await whatsappService.EnqueueTurnoConfirmadoAsync(appointment, "portal_turnos_reserva", cancellationToken);

                if (!await appointmentRepository.TryCommitAsync(cancellationToken))
                {
                    throw new ConflictException("El turno ya no esta disponible.");
                }

                return appointment.ToSummary();
            },
            cancellationToken);
    }

    public async Task<AppointmentSummary> CancelPortalAsync(Guid userId, Guid slotId, string? idempotencyKey, string? motivo, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new UnauthorizedException();
        var patientId = await ResolvePortalPatientIdAsync(user, cancellationToken);
        if (!patientId.HasValue)
        {
            throw new ForbiddenException(ForbiddenMessage);
        }

        return await ExecuteOptionalIdempotentAsync(
            $"portal.turnos.cancelaciones:{slotId}",
            idempotencyKey,
            new { userId, slotId, motivo },
            async () =>
            {
                var appointment = await appointmentRepository.GetByIdAsync(slotId, cancellationToken)
                    ?? throw new NotFoundException(AppointmentNotFoundMessage);

                EnsureNotPast(appointment);

                if (!appointment.IsOccupied() || appointment.PatientId != patientId.Value)
                {
                    throw new ForbiddenException(ForbiddenMessage);
                }

                try
                {
                    await whatsappService.EnqueueTurnoCancelacionAsync(appointment, "portal_turnos_cancelacion", idempotencyKey, cancellationToken);
                    appointment.Cancel(motivo);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ConflictException(exception.Message);
                }

                if (!await appointmentRepository.TryCommitAsync(cancellationToken))
                {
                    throw new ConflictException("No se pudo cancelar el turno por concurrencia.");
                }

                return appointment.ToSummary();
            },
            cancellationToken);
    }

    private async Task<Guid?> ResolvePortalPatientIdAsync(User user, CancellationToken cancellationToken)
    {
        if (user.PatientId.HasValue)
        {
            return user.PatientId;
        }

        var patient = await patientRepository.GetByPortalIdentifierAsync(user.Identifier, cancellationToken);
        if (patient is null)
        {
            patient = await patientRepository.GetByPortalIdentifierAsync(user.Email, cancellationToken);
        }

        if (patient is null || !patient.IsActive)
        {
            return null;
        }

        return patient.Id;
    }

    private async Task<AppointmentSummary> RescheduleSingleAsync(Appointment source, Appointment target, Guid actorUserId, CancellationToken cancellationToken)
    {
        if (!source.IsOccupied() || source.PatientId is null)
        {
            throw new ConflictException("Solo se pueden reprogramar turnos ocupados.");
        }

        if (source.BlockId is not null)
        {
            throw new ConflictException("Los bloques completos no se reprograman como slot individual.");
        }

        if (!target.IsReservable())
        {
            throw new ConflictException("El turno destino ya no esta disponible.");
        }

        await EnsurePatientHasNoConsecutiveAppointmentsAsync(source.PatientId.Value, target.Fecha, target.Hora, source.Id, cancellationToken);

        var sourcePatientId = source.PatientId;
        var sourceTandaId = source.TandaId;

        try
        {
            target.Reserve(source.PatientId.Value, source.Notes, source.EsTanda, source.TandaId, CopyOperativeData(source));
            // Stage source history BEFORE cancel so it still has operative data.
            StageHistory(source, "reprogramado", actorUserId, null, sourcePatientId, sourceTandaId);
            source.Cancel("Reprogramado");
            // Stage target history AFTER reserve so it carries the newly assigned operative data.
            StageHistory(target, "recibido_reprogramado", actorUserId, null, target.PatientId, target.TandaId);
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }

        if (!await appointmentRepository.TryCommitAsync(cancellationToken))
        {
            throw new ConflictException("No se pudo reprogramar el turno por concurrencia.");
        }

        return target.ToSummary();
    }

    private async Task<AppointmentSummary> RescheduleGroupedAsync(Appointment source, Appointment target, Guid actorUserId, CancellationToken cancellationToken)
    {
        var sourceGroup = await GetGroupedSlotsForRescheduleAsync(source, cancellationToken);
        if (sourceGroup.Length == 1)
        {
            return await RescheduleSingleAsync(source, target, actorUserId, cancellationToken);
        }

        if (!target.CameraId.HasValue)
        {
            throw new ConflictException("El slot destino no tiene camara asociada.");
        }

        var targetGroup = await appointmentRepository.GetByBlockAsync(target.Fecha, target.Hora, target.CameraId.Value, cancellationToken);
        var orderedTargetGroup = targetGroup.OrderBy(x => x.Lugar).ToArray();

        if (orderedTargetGroup.Length != sourceGroup.Length)
        {
            throw new ConflictException("El bloque destino no tiene la misma cantidad de slots que el origen.");
        }

        if (orderedTargetGroup.Any(x => !x.IsReservable()))
        {
            throw new ConflictException("El bloque destino ya no esta disponible.");
        }

        var sourcePatientId = sourceGroup[0].PatientId;
        if (!sourcePatientId.HasValue)
        {
            throw new ConflictException("Solo se pueden reprogramar slots ocupados.");
        }

        await EnsurePatientHasNoConsecutiveAppointmentsAsync(sourcePatientId.Value, target.Fecha, target.Hora, source.Id, cancellationToken);

        var newBlockId = Guid.NewGuid();
        var tandaId = sourceGroup.Select(x => x.TandaId).FirstOrDefault(x => x.HasValue) ?? Guid.NewGuid();
        var sourceSnapshots = sourceGroup.Select(s => (s, s.PatientId, s.TandaId)).ToArray();

        // Stage source history BEFORE the loop so each source slot still carries its operative data.
        StageHistoryRange(sourceSnapshots, "reprogramado", actorUserId, null);

        for (var i = 0; i < sourceGroup.Length; i++)
        {
            var sourceSlot = sourceGroup[i];
            var targetSlot = orderedTargetGroup[i];
            try
            {
                targetSlot.Reserve(sourcePatientId.Value, sourceSlot.Notes, sourceSlot.EsTanda, tandaId, CopyOperativeData(sourceSlot));
                targetSlot.AssignBlock(newBlockId, sourceSlot.EsTanda, tandaId, CopyOperativeData(sourceSlot));
                targetSlot.AssignTanda(tandaId);
                sourceSlot.Cancel("Reprogramado");
            }
            catch (InvalidOperationException exception)
            {
                throw new ConflictException(exception.Message);
            }
        }

        // Stage target history AFTER all slots are reserved.
        StageHistoryRange(orderedTargetGroup.Select(s => (s, s.PatientId, s.TandaId)).ToArray(), "recibido_reprogramado", actorUserId, null);

        if (!await appointmentRepository.TryCommitAsync(cancellationToken))
        {
            throw new ConflictException("No se pudo reprogramar el turno por concurrencia.");
        }

        return orderedTargetGroup[0].ToSummary();
    }

    private async Task<Appointment[]> GetGroupedSlotsForRescheduleAsync(Appointment source, CancellationToken cancellationToken)
    {
        if (source.TandaId.HasValue)
        {
            var byTanda = (await appointmentRepository.GetByTandaIdAsync(source.TandaId.Value, cancellationToken)).ToArray();
            if (byTanda.Length > 1)
            {
                return byTanda;
            }
        }

        if (source.BlockId.HasValue)
        {
            if (!source.CameraId.HasValue)
            {
                throw new ConflictException("El slot origen no tiene camara asociada.");
            }

            var byBlock = (await appointmentRepository.GetByBlockAsync(source.Fecha, source.Hora, source.CameraId.Value, cancellationToken)).ToArray();
            if (byBlock.Length > 1)
            {
                return byBlock;
            }
        }

        return [source];
    }

    private async Task ValidateMedicoUserAsync(Guid? medicoUserId, CancellationToken cancellationToken)
    {
        if (!medicoUserId.HasValue) return;
        var medico = await userRepository.GetByIdAsync(medicoUserId.Value, cancellationToken)
            ?? throw new NotFoundException("Médico no encontrado.");
        if (!medico.IsActive)
            throw new ConflictException("El médico no se encuentra activo.");
        if (!medico.Roles.Any(r => r.Code == "medico"))
            throw new ConflictException("El usuario no tiene rol de médico.");
    }

    private static AppointmentOperativeData BuildOperativeData(AssignAppointmentCommand command) =>
        new(
            command.ReferidoTercero,
            command.ReferenteId,
            command.ModalidadCobro,
            command.ObraSocialId,
            command.NumeroAutorizacion,
            command.SesionesAutorizadas,
            command.CicloObraSocialId,
            command.IniciarNuevoCicloObraSocial,
            command.ConvenioCorroborado,
            command.MedicoId,
            command.EsNuevoIngreso,
            command.EsMonoxido,
            command.MonoxidoOrdenMedica,
            command.MonoxidoResumenClinico,
            command.MedicoUserId);

    private static AppointmentOperativeData BuildOperativeData(HoldAppointmentCommand command) =>
        new(
            command.ReferidoTercero,
            command.ReferenteId,
            command.ModalidadCobro,
            command.ObraSocialId,
            command.NumeroAutorizacion,
            command.SesionesAutorizadas,
            command.CicloObraSocialId,
            command.IniciarNuevoCicloObraSocial,
            command.ConvenioCorroborado,
            command.MedicoId,
            command.EsNuevoIngreso,
            command.EsMonoxido,
            command.MonoxidoOrdenMedica,
            command.MonoxidoResumenClinico,
            command.MedicoUserId);

    private static AppointmentOperativeData BuildOperativeData(AssignBlockAppointmentsCommand command) =>
        new(
            command.ReferidoTercero,
            command.ReferenteId,
            command.ModalidadCobro,
            command.ObraSocialId,
            command.NumeroAutorizacion,
            command.SesionesAutorizadas,
            command.CicloObraSocialId,
            command.IniciarNuevoCicloObraSocial,
            command.ConvenioCorroborado,
            command.MedicoId,
            command.EsNuevoIngreso,
            command.EsMonoxido,
            command.MonoxidoOrdenMedica,
            command.MonoxidoResumenClinico,
            command.MedicoUserId);

    private static AppointmentOperativeData CopyOperativeData(Appointment appointment) =>
        new(
            appointment.ReferidoTercero,
            appointment.ReferenteId,
            appointment.ModalidadCobro,
            appointment.ObraSocialId,
            appointment.NumeroAutorizacion,
            appointment.SesionesAutorizadas,
            appointment.CicloObraSocialId,
            appointment.IniciarNuevoCicloObraSocial,
            appointment.ConvenioCorroborado,
            appointment.MedicoId,
            appointment.EsNuevoIngreso,
            appointment.EsMonoxido,
            appointment.MonoxidoOrdenMedica,
            appointment.MonoxidoResumenClinico,
            appointment.MedicoUserId);

    private async Task<IReadOnlyCollection<AppointmentSummary>> FilterAndMapAsync(IEnumerable<Appointment> appointments, CancellationToken cancellationToken)
    {
        var activeHours = await GetActiveHoursAsync(cancellationToken);
        var filtered = FilterAppointmentsByActiveHours(appointments, activeHours);
        if (filtered.Length == 0)
        {
            return [];
        }

        var historyBySlot = await LoadHistoryBySlotAsync(filtered, cancellationToken);
        var userCache = new Dictionary<Guid, GuidLookupSummary?>();
        var summaries = new List<AppointmentSummary>(filtered.Length);

        foreach (var appointment in filtered)
        {
            var summary = await BuildAppointmentSummaryAsync(appointment, historyBySlot, userCache, cancellationToken);
            summaries.Add(summary);
        }

        return summaries;
    }

    private async Task<HashSet<string>> GetActiveHoursAsync(CancellationToken cancellationToken) =>
        (await scheduleHourRepository.GetAsync(cancellationToken))
        .Where(x => x.Activo)
        .Select(x => x.Hora)
        .ToHashSet(StringComparer.Ordinal);

    private static Appointment[] FilterAppointmentsByActiveHours(IEnumerable<Appointment> appointments, HashSet<string> activeHours) =>
        appointments
            .Where(x => activeHours.Contains(x.Hora.ToString("HH:mm")))
            .ToArray();

    private readonly record struct TurnoLookupIds(
        Guid[] PatientIds,
        int[] MedicoIds,
        int[] ReferenteIds,
        int[] ObraSocialIds,
        int[] CameraIds);

    private sealed record TurnoEnrichmentLookups(
        Dictionary<Guid, Patient> PatientById,
        Dictionary<int, Medico> MedicoById,
        Dictionary<int, Referente> ReferenteById,
        Dictionary<int, ObraSocial> ObraSocialById,
        Dictionary<int, Camera> CameraById,
        Dictionary<Guid, User> UserById,
        Dictionary<Guid, BlockHistory> LatestValidationBySlot);

    private static TurnoLookupIds CollectLookupIds(IEnumerable<Appointment> appointments)
    {
        var patientIds = appointments
            .Where(a => a.PatientId.HasValue)
            .Select(a => a.PatientId!.Value)
            .Distinct()
            .ToArray();
        var medicoIds = appointments
            .Where(a => a.MedicoId.HasValue)
            .Select(a => a.MedicoId!.Value)
            .Distinct()
            .ToArray();
        var referenteIds = appointments
            .Where(a => a.ReferenteId.HasValue)
            .Select(a => a.ReferenteId!.Value)
            .Distinct()
            .ToArray();
        var obraSocialIds = appointments
            .Where(a => a.ObraSocialId.HasValue)
            .Select(a => a.ObraSocialId!.Value)
            .Distinct()
            .ToArray();
        var cameraIds = appointments
            .Where(a => a.CameraId.HasValue)
            .Select(a => a.CameraId!.Value)
            .Distinct()
            .ToArray();

        return new TurnoLookupIds(patientIds, medicoIds, referenteIds, obraSocialIds, cameraIds);
    }

    private async Task<TurnoEnrichmentLookups> LoadTurnoEnrichmentLookupsAsync(
        TurnoLookupIds ids,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        CancellationToken cancellationToken)
    {
        var patients = ids.PatientIds.Length > 0
            ? await patientRepository.GetByIdsAsync(ids.PatientIds, cancellationToken)
            : [];
        var medicos = ids.MedicoIds.Length > 0
            ? await medicoRepository.GetByIdsAsync(ids.MedicoIds, cancellationToken)
            : [];
        var referentes = ids.ReferenteIds.Length > 0
            ? await referenteRepository.GetByIdsAsync(ids.ReferenteIds, cancellationToken)
            : [];
        var obrasSociales = ids.ObraSocialIds.Length > 0
            ? await obraSocialRepository.GetByIdsAsync(ids.ObraSocialIds, cancellationToken)
            : [];
        var cameras = ids.CameraIds.Length > 0
            ? await cameraRepository.GetAsync(cancellationToken)
            : [];
        var blockHistory = await appointmentRepository.GetBlockHistoryByRangeAsync(fechaInicio, fechaFin, cancellationToken);

        var userProfileIds = blockHistory
            .Where(b => b.ObraSocialValidadaPor.HasValue)
            .Select(b => b.ObraSocialValidadaPor!.Value)
            .Distinct()
            .ToArray();
        var users = userProfileIds.Length > 0
            ? await userRepository.GetBasicByIdsAsync(userProfileIds, cancellationToken)
            : [];

        var latestValidationBySlot = blockHistory
            .Where(b => b.SlotId.HasValue && (b.ObraSocialValidadaPor.HasValue || b.ObraSocialValidadaAt.HasValue))
            .GroupBy(b => b.SlotId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(b => b.ObraSocialValidadaAt ?? b.CreatedAt).First());

        return new TurnoEnrichmentLookups(
            patients.ToDictionary(p => p.Id),
            medicos.ToDictionary(m => m.Id),
            referentes.ToDictionary(r => r.Id),
            obrasSociales.ToDictionary(o => o.Id),
            cameras.Where(c => c.Activa).ToDictionary(c => c.Id),
            users.ToDictionary(u => u.Id),
            latestValidationBySlot);
    }

    private static TurnoEnrichedSummary[] MapTurnoEnrichedSummaries(
        IEnumerable<Appointment> appointments,
        TurnoEnrichmentLookups lookups) =>
        appointments.Select(appointment => MapTurnoEnrichedSummary(appointment, lookups)).ToArray();

    private static TurnoEnrichedSummary MapTurnoEnrichedSummary(Appointment appointment, TurnoEnrichmentLookups lookups)
    {
        var hasValidation = lookups.LatestValidationBySlot.TryGetValue(appointment.Id, out var validation);
        var latestValidation = validation;
        var patient = MapPacienteEnrichedSummary(appointment, lookups);
        var medico = MapMedicoEnrichedSummary(appointment, lookups);
        var referente = MapReferenteEnrichedSummary(appointment, lookups);
        var camara = MapCamaraEnrichedSummary(appointment, lookups);
        var obraSocial = MapObraSocialEnrichedSummary(appointment, lookups);
        var obraSocialValidadaPorPerfil = MapObraSocialValidadaPorPerfilSummary(hasValidation, latestValidation, lookups);

        return new TurnoEnrichedSummary(
            Id: appointment.Id,
            Fecha: appointment.Fecha,
            Hora: appointment.Hora,
            CamaraId: appointment.CameraId,
            Lugar: appointment.Lugar,
            Estado: appointment.Status.ToString().ToLowerInvariant(),
            PacienteId: appointment.PatientId,
            EsTanda: appointment.EsTanda,
            TandaId: appointment.TandaId,
            EsBloqueCompleto: appointment.EsBloqueCompleto,
            ReferidoTercero: appointment.ReferidoTercero,
            ReferenteId: appointment.ReferenteId,
            ModalidadCobro: NormalizeModalidadCobro(appointment.ModalidadCobro),
            ObraSocialId: appointment.ObraSocialId,
            NumeroAutorizacion: appointment.NumeroAutorizacion,
            SesionesAutorizadas: NormalizeSesionesAutorizadas(appointment.SesionesAutorizadas),
            CicloObraSocialId: appointment.CicloObraSocialId,
            MedicoId: appointment.MedicoId,
            EsNuevoIngreso: appointment.EsNuevoIngreso,
            ObraSocialValidadaPor: hasValidation ? latestValidation?.ObraSocialValidadaPor : null,
            ObraSocialValidadaAt: hasValidation ? latestValidation?.ObraSocialValidadaAt : null,
            Paciente: patient,
            Medico: medico,
            Referente: referente,
            Camara: camara,
            ObraSocial: obraSocial,
            ObraSocialValidadaPorPerfil: obraSocialValidadaPorPerfil);
    }

    private static string? NormalizeModalidadCobro(string modalidadCobro) =>
        modalidadCobro == "particular" ? null : modalidadCobro;

    private static int? NormalizeSesionesAutorizadas(int? sesionesAutorizadas) =>
        sesionesAutorizadas is null or 0 ? null : sesionesAutorizadas;

    private static PacienteEnrichedSummary? MapPacienteEnrichedSummary(Appointment appointment, TurnoEnrichmentLookups lookups)
    {
        if (!appointment.PatientId.HasValue || !lookups.PatientById.TryGetValue(appointment.PatientId.Value, out var patient))
        {
            return null;
        }

        return new PacienteEnrichedSummary(patient.Id, patient.Nombre, patient.Email, patient.ObraSocialId);
    }

    private static MedicoEnrichedSummary? MapMedicoEnrichedSummary(Appointment appointment, TurnoEnrichmentLookups lookups)
    {
        if (!appointment.MedicoId.HasValue || !lookups.MedicoById.TryGetValue(appointment.MedicoId.Value, out var medico))
        {
            return null;
        }

        return new MedicoEnrichedSummary(medico.Id, medico.Nombre, medico.Activo);
    }

    private static ReferenteEnrichedSummary? MapReferenteEnrichedSummary(Appointment appointment, TurnoEnrichmentLookups lookups)
    {
        if (!appointment.ReferenteId.HasValue || !lookups.ReferenteById.TryGetValue(appointment.ReferenteId.Value, out var referente))
        {
            return null;
        }

        return new ReferenteEnrichedSummary(referente.Id, referente.Nombre, referente.Tipo, referente.Activo);
    }

    private static CamaraEnrichedSummary? MapCamaraEnrichedSummary(Appointment appointment, TurnoEnrichmentLookups lookups)
    {
        if (!appointment.CameraId.HasValue || !lookups.CameraById.TryGetValue(appointment.CameraId.Value, out var camara))
        {
            return null;
        }

        return new CamaraEnrichedSummary(camara.Id, camara.Nombre, camara.Capacidad);
    }

    private static ObraSocialEnrichedSummary? MapObraSocialEnrichedSummary(Appointment appointment, TurnoEnrichmentLookups lookups)
    {
        if (!appointment.ObraSocialId.HasValue || !lookups.ObraSocialById.TryGetValue(appointment.ObraSocialId.Value, out var obraSocial))
        {
            return null;
        }

        return new ObraSocialEnrichedSummary(obraSocial.Id, obraSocial.Nombre, obraSocial.Activa, obraSocial.TieneConvenio);
    }

    private static UserBasicLookupSummary? MapObraSocialValidadaPorPerfilSummary(
        bool hasValidation,
        BlockHistory? latestValidation,
        TurnoEnrichmentLookups lookups)
    {
        if (!hasValidation || latestValidation?.ObraSocialValidadaPor is not Guid validadaPor)
        {
            return null;
        }

        return lookups.UserById.TryGetValue(validadaPor, out var user)
            ? new UserBasicLookupSummary(user.Id, user.Nombre)
            : null;
    }

    private async Task<Dictionary<Guid, BlockHistory[]>> LoadHistoryBySlotAsync(
        IReadOnlyCollection<Appointment> appointments,
        CancellationToken cancellationToken)
    {
        var minFecha = appointments.Min(x => x.Fecha);
        var maxFecha = appointments.Max(x => x.Fecha);
        return (await blockHistoryRepository.GetByRangeAsync(minFecha, maxFecha, null, cancellationToken))
            .Where(x => x.SlotId.HasValue)
            .GroupBy(x => x.SlotId!.Value)
            .ToDictionary(x => x.Key, x => x.ToArray());
    }

    private async Task<AppointmentSummary> BuildAppointmentSummaryAsync(
        Appointment appointment,
        Dictionary<Guid, BlockHistory[]> historyBySlot,
        IDictionary<Guid, GuidLookupSummary?> userCache,
        CancellationToken cancellationToken)
    {
        var patient = appointment.PatientId.HasValue
            ? await patientRepository.GetByIdAsync(appointment.PatientId.Value, cancellationToken)
            : null;
        var medico = appointment.MedicoId.HasValue
            ? await medicoRepository.GetByIdAsync(appointment.MedicoId.Value, cancellationToken)
            : null;
        var referente = appointment.ReferenteId.HasValue
            ? await referenteRepository.GetByIdAsync(appointment.ReferenteId.Value, cancellationToken)
            : null;
        var obraSocial = appointment.ObraSocialId.HasValue
            ? await obraSocialRepository.GetByIdAsync(appointment.ObraSocialId.Value, cancellationToken)
            : null;
        var apartadoPorPerfil = await GetUserSummaryAsync(appointment.ApartadoPorUserId, userCache, cancellationToken);
        historyBySlot.TryGetValue(appointment.Id, out var slotHistory);
        var createdAt = slotHistory?.Min(x => x.CreatedAt);
        var obraSocialValidated = slotHistory?
            .Where(x => x.ObraSocialValidadaPor.HasValue || x.ObraSocialValidadaAt.HasValue)
            .OrderByDescending(x => x.ObraSocialValidadaAt ?? x.CreatedAt)
            .FirstOrDefault();
        var obraSocialValidadaPorPerfil = await GetUserSummaryAsync(
            obraSocialValidated?.ObraSocialValidadaPor,
            userCache,
            cancellationToken);

        var baseSummary = appointment.ToSummary();
        return baseSummary with
        {
            CreatedAt = createdAt,
            ObraSocialValidadaPor = obraSocialValidated?.ObraSocialValidadaPor,
            ObraSocialValidadaAt = obraSocialValidated?.ObraSocialValidadaAt,
            Paciente = patient is null ? null : new GuidLookupSummary(patient.Id, patient.Nombre, patient.DocumentoIdentidad, patient.Email, patient.IsActive),
            Medico = medico is null ? null : new IntLookupSummary(medico.Id, medico.Nombre, medico.Activo.ToString(), medico.Activo),
            Referente = referente is null ? null : new IntLookupSummary(referente.Id, referente.Nombre, referente.Tipo, referente.Activo),
            ObraSocial = obraSocial is null ? null : new ObraSocialSummaryDto(obraSocial.Id, obraSocial.Nombre, obraSocial.Activa, obraSocial.TieneConvenio, obraSocial.Orden, obraSocial.Abreviatura, obraSocial.CreatedAt),
            ApartadoPorPerfil = apartadoPorPerfil,
            ObraSocialValidadaPorPerfil = obraSocialValidadaPorPerfil,
        };
    }

    private async Task<GuidLookupSummary?> GetUserSummaryAsync(
        Guid? userId,
        IDictionary<Guid, GuidLookupSummary?> userCache,
        CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        if (userCache.TryGetValue(userId.Value, out var cached))
        {
            return cached;
        }

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        var summary = user is null ? null : new GuidLookupSummary(user.Id, user.Nombre ?? string.Empty);
        userCache[userId.Value] = summary;
        return summary;
    }

    private async Task<Dictionary<DateOnly, HashSet<int>>> BuildBlockedHoursByDateAsync(
        Guid? patientId,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        CancellationToken cancellationToken)
    {
        var blockedHoursByDate = new Dictionary<DateOnly, HashSet<int>>();
        if (!patientId.HasValue)
        {
            return blockedHoursByDate;
        }

        var patientAppointments = await appointmentRepository.GetActivosByPacienteAsync(
            patientId.Value,
            fechaInicio,
            cancellationToken);
        foreach (var group in patientAppointments
                     .Where(x => x.Fecha >= fechaInicio && x.Fecha <= fechaFin && x.IsOccupied())
                     .GroupBy(x => x.Fecha))
        {
            var blockedHours = new HashSet<int>();
            foreach (var appointment in group)
            {
                var hour = appointment.Hora.Hour;
                blockedHours.Add(hour);
                if (hour > 0) blockedHours.Add(hour - 1);
                if (hour < 23) blockedHours.Add(hour + 1);
            }

            blockedHoursByDate[group.Key] = blockedHours;
        }

        return blockedHoursByDate;
    }

    private static Dictionary<(DateOnly Fecha, int Hora, int CameraId), Appointment[]> BuildSlotsByCell(
        IEnumerable<Appointment> appointments,
        Dictionary<int, Camera> activeCameras) =>
        appointments
            .Where(x => x.CameraId.HasValue && activeCameras.ContainsKey(x.CameraId.Value))
            .GroupBy(x => (x.Fecha, Hora: x.Hora.Hour, CameraId: x.CameraId!.Value))
            .ToDictionary(g => g.Key, g => g.ToArray());

    private static List<TandaAvailabilityAggregatedSummary> BuildAggregatedAvailability(
        DateOnly fechaInicio,
        DateOnly fechaFin,
        IReadOnlyCollection<string> activeHours,
        Dictionary<int, Camera> activeCameras,
        Dictionary<DateOnly, HashSet<int>> blockedHoursByDate,
        Dictionary<(DateOnly Fecha, int Hora, int CameraId), Appointment[]> slotsByCell)
    {
        var results = new List<TandaAvailabilityAggregatedSummary>();
        foreach (var fecha in EachDay(fechaInicio, fechaFin))
        {
            blockedHoursByDate.TryGetValue(fecha, out var blockedHoursForDate);
            foreach (var hour in activeHours)
            {
                var hourInt = int.Parse(hour[..2]);
                var isBlockedForPatient = blockedHoursForDate?.Contains(hourInt) == true;
                results.AddRange(BuildAvailabilityForHour(fecha, hourInt, isBlockedForPatient, activeCameras, slotsByCell));
            }
        }

        return results;
    }

    private static IEnumerable<TandaAvailabilityAggregatedSummary> BuildAvailabilityForHour(
        DateOnly fecha,
        int hourInt,
        bool isBlockedForPatient,
        Dictionary<int, Camera> activeCameras,
        Dictionary<(DateOnly Fecha, int Hora, int CameraId), Appointment[]> slotsByCell)
    {
        var hour = new TimeOnly(hourInt, 0);
        foreach (var (cameraId, camera) in activeCameras)
        {
            var key = (fecha, hourInt, cameraId);
            var cellSlots = slotsByCell.TryGetValue(key, out var slots) ? slots : [];
            var libresCount = isBlockedForPatient ? 0 : cellSlots.Count(x => x.IsReservable());
            var tieneDisponibilidad = libresCount > 0;
            var tieneBloqueCompletoPosible = libresCount == camera.Capacidad && camera.Capacidad > 0;

            yield return new TandaAvailabilityAggregatedSummary(
                fecha,
                hour,
                cameraId,
                camera.Nombre,
                camera.Capacidad,
                libresCount,
                tieneDisponibilidad,
                tieneBloqueCompletoPosible,
                isBlockedForPatient);
        }
    }

    private async Task EnsurePatientHasNoConsecutiveAppointmentsAsync(Guid patientId, DateOnly fecha, TimeOnly hora, Guid? ignoreAppointmentId, CancellationToken cancellationToken)
    {
        var occupiedAppointments = await appointmentRepository.GetOccupiedByPacienteOnDateAsync(patientId, fecha, cancellationToken);
        if (occupiedAppointments.Any(x => x.Id != ignoreAppointmentId && Math.Abs(ToMinutes(x.Hora) - ToMinutes(hora)) <= 60))
        {
            throw new ConflictException("No puedes reservar turnos consecutivos en el mismo dia");
        }
    }

    private void EnsureNotPast(Appointment appointment)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.UtcNow.UtcDateTime, GetArgentinaTimeZone()).Date);
        if (appointment.Fecha < today)
        {
            throw new ConflictException("No se pueden operar turnos pasados.");
        }
    }

    private static TimeZoneInfo GetArgentinaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private async Task<AppointmentSummary> ExecuteOptionalIdempotentAsync<TPayload>(
        string operation,
        string? idempotencyKey,
        TPayload payload,
        Func<Task<AppointmentSummary>> action,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return await action();
        }

        return await ExecuteIdempotentAsync(operation, idempotencyKey, payload, action, cancellationToken);
    }

    private async Task<AppointmentSummary> ExecuteIdempotentAsync<TPayload>(
        string operation,
        string idempotencyKey,
        TPayload payload,
        Func<Task<AppointmentSummary>> action,
        CancellationToken cancellationToken)
    {
        var requestHash = ComputeHash(payload);
        var reservation = await idempotencyStore.ReserveAsync(operation, idempotencyKey, requestHash, cancellationToken);

        switch (reservation.State)
        {
            case IdempotencyReservationState.Completed:
                return JsonSerializer.Deserialize<AppointmentSummary>(reservation.ResponsePayload ?? string.Empty, SerializerOptions)
                    ?? throw new ConflictException("No se pudo reconstruir la respuesta idempotente.");
            case IdempotencyReservationState.Pending:
                throw new ConflictException("La operacion aun esta en proceso.", "operation_pending");
            case IdempotencyReservationState.Mismatch:
                throw new ConflictException("La Idempotency-Key ya fue usada con otro payload.", "idempotency_mismatch");
            case IdempotencyReservationState.Acquired:
                try
                {
                    var result = await action();
                    await idempotencyStore.CompleteAsync(operation, idempotencyKey, JsonSerializer.Serialize(result, SerializerOptions), cancellationToken);
                    return result;
                }
                catch
                {
                    await idempotencyStore.FailAsync(operation, idempotencyKey, cancellationToken);
                    throw;
                }
            default:
                throw new ConflictException("Estado de idempotencia invalido.");
        }
    }

    private async Task<IReadOnlyCollection<AppointmentSummary>> ExecuteIdempotentCollectionAsync<TPayload>(
        string operation,
        string idempotencyKey,
        TPayload payload,
        Func<Task<IReadOnlyCollection<AppointmentSummary>>> action,
        CancellationToken cancellationToken)
    {
        var requestHash = ComputeHash(payload);
        var reservation = await idempotencyStore.ReserveAsync(operation, idempotencyKey, requestHash, cancellationToken);

        switch (reservation.State)
        {
            case IdempotencyReservationState.Completed:
                return JsonSerializer.Deserialize<AppointmentSummary[]>(reservation.ResponsePayload ?? string.Empty, SerializerOptions)
                    ?? throw new ConflictException("No se pudo reconstruir la respuesta idempotente.");
            case IdempotencyReservationState.Pending:
                throw new ConflictException("La operacion aun esta en proceso.", "operation_pending");
            case IdempotencyReservationState.Mismatch:
                throw new ConflictException("La Idempotency-Key ya fue usada con otro payload.", "idempotency_mismatch");
            case IdempotencyReservationState.Acquired:
                try
                {
                    var result = await action();
                    await idempotencyStore.CompleteAsync(operation, idempotencyKey, JsonSerializer.Serialize(result, SerializerOptions), cancellationToken);
                    return result;
                }
                catch
                {
                    await idempotencyStore.FailAsync(operation, idempotencyKey, cancellationToken);
                    throw;
                }
            default:
                throw new ConflictException("Estado de idempotencia invalido.");
        }
    }

    private static void ValidateRequiredIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ValidationException("El header Idempotency-Key es obligatorio para esta operacion.");
        }
    }

    private static string ComputeHash<TPayload>(TPayload payload)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static int ToMinutes(TimeOnly hora) => hora.Hour * 60 + hora.Minute;

    public async Task<IReadOnlyCollection<TandaAvailabilitySummary>> GetTandaAvailabilityAsync(DateOnly fechaInicio, DateOnly fechaFin, Guid? patientId, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByRangeAsync(fechaInicio, fechaFin, null, null, cancellationToken);
        if (patientId.HasValue)
        {
            appointments = appointments.Where(x => x.PatientId == patientId || x.PatientId is null).ToArray();
        }

        return appointments
            .GroupBy(x => x.Fecha)
            .OrderBy(g => g.Key)
            .Select(group => new TandaAvailabilitySummary(
                group.Key,
                group.Count(),
                group.Count(x => x.Status == AppointmentStatus.Ocupado),
                group.Count(x => x.IsReservable())))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TandaAvailabilityDetailSummary>> GetTandaAvailabilityDetailAsync(DateOnly fechaInicio, DateOnly fechaFin, Guid? patientId, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByRangeAsync(fechaInicio, fechaFin, null, null, cancellationToken);
        if (patientId.HasValue)
        {
            appointments = appointments.Where(x => x.PatientId == patientId || x.PatientId is null).ToArray();
        }

        return appointments
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ThenBy(x => x.CameraId)
            .ThenBy(x => x.Lugar)
            .Select(x => new TandaAvailabilityDetailSummary(x.Fecha, x.Hora, x.CameraId ?? 0, x.Lugar, x.Status.ToString().ToLowerInvariant(), x.TandaId, x.PatientId, x.EsBloqueCompleto))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TandaAvailabilityAggregatedSummary>> GetTandaAvailabilityAggregatedAsync(
        DateOnly fechaInicio, DateOnly fechaFin, Guid? patientId, CancellationToken cancellationToken)
    {
        var appointments = await appointmentRepository.GetByRangeAsync(fechaInicio, fechaFin, null, null, cancellationToken);
        var activeCameras = await GetActiveCamerasAsync(cancellationToken);
        var activeHours = await GetActiveHoursAsync(cancellationToken);
        var horasBloqueadasPorFecha = await BuildBlockedHoursByDateAsync(patientId, fechaInicio, fechaFin, cancellationToken);
        var slotsByCell = BuildSlotsByCell(appointments, activeCameras);

        var results = BuildAggregatedAvailability(
            fechaInicio,
            fechaFin,
            activeHours,
            activeCameras,
            horasBloqueadasPorFecha,
            slotsByCell);

        return results
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Hora)
            .ThenBy(x => x.CamaraId)
            .ToArray();
    }

    private static IEnumerable<DateOnly> EachDay(DateOnly from, DateOnly to)
    {
        for (var day = from; day <= to; day = day.AddDays(1))
            yield return day;
    }

    private async Task<Dictionary<int, Camera>> GetActiveCamerasAsync(CancellationToken cancellationToken)
    {
        var cameras = await cameraRepository.GetAsync(cancellationToken);
        return cameras.Where(x => x.Activa).ToDictionary(x => x.Id);
    }

    public async Task<IReadOnlyCollection<AppointmentSummary>> GetSlotsByTandaAsync(Guid tandaId, CancellationToken cancellationToken) =>
        (await appointmentRepository.GetByTandaIdAsync(tandaId, cancellationToken)).Select(x => x.ToSummary()).ToArray();

    public async Task<IReadOnlyCollection<AppointmentSummary>> GetActiveSlotsByTandaAsync(Guid tandaId, CancellationToken cancellationToken) =>
        (await appointmentRepository.GetByTandaIdAsync(tandaId, cancellationToken))
            .Where(x => x.Status == AppointmentStatus.Ocupado)
            .Select(x => x.ToSummary())
            .ToArray();

    public async Task<IReadOnlyCollection<BlockHistorySummary>> GetBlockHistoryAsync(DateOnly fecha, TimeOnly hora, int? camaraId, CancellationToken cancellationToken)
    {
        var histories = await blockHistoryRepository.GetByBlockAsync(fecha, hora, camaraId, cancellationToken);
        return await MapBlockHistoriesAsync(histories, cancellationToken);
    }

    public async Task<IReadOnlyCollection<BlockHistorySummary>> GetBlockHistoryBySlotAsync(Guid slotId, CancellationToken cancellationToken)
    {
        var histories = await blockHistoryRepository.GetBySlotAsync(slotId, cancellationToken);
        return await MapBlockHistoriesAsync(histories, cancellationToken);
    }

    public async Task<IReadOnlyCollection<BlockHistorySummary>> GetBlockHistoryByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, int? camaraId, CancellationToken cancellationToken)
    {
        var histories = await blockHistoryRepository.GetByRangeAsync(fechaInicio, fechaFin, camaraId, cancellationToken);
        return await MapBlockHistoriesAsync(histories, cancellationToken);
    }

    public async Task<int> RegisterBlockHistoryAsync(Guid actorUserId, IReadOnlyCollection<BlockHistoryWriteCommand> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return 0;
        }

        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken) ?? throw new UnauthorizedException();
        if (!actor.IsStaff)
        {
            throw new ForbiddenException(ForbiddenMessage);
        }

        var histories = entries.Select(entry =>
        {
            if (string.IsNullOrWhiteSpace(entry.Accion))
            {
                throw new ValidationException("La accion del historial es obligatoria.");
            }

            return new BlockHistory(new BlockHistoryCreateParams(
                Guid.NewGuid(),
                entry.Fecha,
                entry.Hora,
                entry.CamaraId,
                entry.SlotId,
                entry.Lugar,
                entry.Accion.Trim(),
                entry.PacienteId,
                actorUserId,
                entry.Motivo,
                false,
                ModalidadCobroConstants.Default,
                null,
                null,
                null,
                null,
                null,
                false,
                null,
                null,
                null,
                null));
        }).ToArray();

        await blockHistoryRepository.AddRangeAsync(histories, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return histories.Length;
    }

    public async Task<AppointmentSummary> UpdateOperativeAsync(Guid actorUserId, Guid slotId, AppointmentOperativeCommand command, CancellationToken cancellationToken)
    {
        await RequireActorAsync(actorUserId, "turnos.asignar", cancellationToken);
        var appointment = await appointmentRepository.GetByIdAsync(slotId, cancellationToken) ?? throw new NotFoundException(AppointmentNotFoundMessage);
        if (!appointment.IsOccupied())
        {
            throw new ConflictException("El turno ocupado no encontrado.");
        }

        appointment.UpdateOperativeData(MapOperative(command));
        if (!await appointmentRepository.TryCommitAsync(cancellationToken))
        {
            throw new ConflictException("No se pudo actualizar el turno por concurrencia.");
        }
        return appointment.ToSummary();
    }

    public async Task<IReadOnlyCollection<AppointmentSummary>> UpdateOperativeByTandaAsync(Guid actorUserId, Guid tandaId, AppointmentOperativeCommand command, CancellationToken cancellationToken)
    {
        await RequireActorAsync(actorUserId, "turnos.tanda", cancellationToken);
        var appointments = (await appointmentRepository.GetByTandaIdAsync(tandaId, cancellationToken)).Where(x => x.Status == AppointmentStatus.Ocupado).ToArray();
        foreach (var appointment in appointments)
        {
            appointment.UpdateOperativeData(MapOperative(command));
        }

        if (!await appointmentRepository.TryCommitAsync(cancellationToken))
        {
            throw new ConflictException("No se pudo actualizar la tanda por concurrencia.");
        }
        return appointments.Select(x => x.ToSummary()).ToArray();
    }

    private async Task<User> RequireActorAsync(Guid actorUserId, string permission, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken) ?? throw new UnauthorizedException();
        if (!actor.IsStaff || !actor.HasPermission(permission))
        {
            throw new ForbiddenException(ForbiddenMessage);
        }

        return actor;
    }

    private async Task<IReadOnlyCollection<BlockHistorySummary>> MapBlockHistoriesAsync(IEnumerable<BlockHistory> histories, CancellationToken cancellationToken)
    {
        var results = new List<BlockHistorySummary>();
        foreach (var history in histories)
        {
            results.Add(await MapBlockHistoryAsync(history, cancellationToken));
        }
        return results;
    }

    private async Task<BlockHistorySummary> MapBlockHistoryAsync(BlockHistory history, CancellationToken cancellationToken)
    {
        var patient = history.PacienteId.HasValue
            ? await patientRepository.GetByIdAsync(history.PacienteId.Value, cancellationToken)
            : null;
        var medico = history.MedicoId.HasValue
            ? await medicoRepository.GetByIdAsync(history.MedicoId.Value, cancellationToken)
            : null;
        var referente = history.ReferenteId.HasValue
            ? await referenteRepository.GetByIdAsync(history.ReferenteId.Value, cancellationToken)
            : null;
        var obraSocial = history.ObraSocialId.HasValue
            ? await obraSocialRepository.GetByIdAsync(history.ObraSocialId.Value, cancellationToken)
            : null;
        var realizadoPor = history.RealizadoPor.HasValue
            ? await userRepository.GetByIdAsync(history.RealizadoPor.Value, cancellationToken)
            : null;
        var validatedBy = history.ObraSocialValidadaPor.HasValue
            ? await userRepository.GetByIdAsync(history.ObraSocialValidadaPor.Value, cancellationToken)
            : null;

        return new BlockHistorySummary(
            history.Id,
            history.Fecha,
            history.Hora,
            history.CamaraId,
            history.SlotId,
            history.Lugar,
            history.Accion,
            history.PacienteId,
            history.RealizadoPor,
            history.Motivo,
            history.ReferidoTercero,
            history.ModalidadCobro,
            history.ObraSocialId,
            history.NumeroAutorizacion,
            history.ObraSocialValidadaPor,
            history.ObraSocialValidadaAt,
            history.MedicoId,
            history.EsNuevoIngreso,
            history.ReferenteId,
            history.TandaId,
            history.SesionesAutorizadas,
            history.CicloObraSocialId,
            history.CreatedAt,
            patient is null ? null : new GuidLookupSummary(patient.Id, patient.Nombre, patient.DocumentoIdentidad, patient.Email, patient.IsActive),
            medico is null ? null : new IntLookupSummary(medico.Id, medico.Nombre, medico.Activo.ToString(), medico.Activo),
            referente is null ? null : new IntLookupSummary(referente.Id, referente.Nombre, referente.Tipo, referente.Activo),
            obraSocial is null ? null : new ObraSocialSummaryDto(obraSocial.Id, obraSocial.Nombre, obraSocial.Activa, obraSocial.TieneConvenio, obraSocial.Orden, obraSocial.Abreviatura, obraSocial.CreatedAt),
            realizadoPor is null ? null : new GuidLookupSummary(realizadoPor.Id, realizadoPor.Nombre ?? string.Empty),
            validatedBy is null ? null : new GuidLookupSummary(validatedBy.Id, validatedBy.Nombre ?? string.Empty));
    }

    private static AppointmentOperativeData MapOperative(AppointmentOperativeCommand command) =>
        new(
            command.ReferidoTercero,
            command.ReferenteId,
            command.ModalidadCobro,
            command.ObraSocialId,
            command.NumeroAutorizacion,
            command.SesionesAutorizadas,
            command.CicloObraSocialId,
            command.IniciarNuevoCicloObraSocial,
            command.ConvenioCorroborado,
            command.MedicoId,
            command.EsNuevoIngreso,
            command.EsMonoxido,
            command.MonoxidoOrdenMedica,
            command.MonoxidoResumenClinico,
            command.MedicoUserId);

    /// <summary>
    /// Stages a single slot history entry in the DbContext without persisting.
    /// The caller is responsible for the subsequent TryCommitAsync / SaveChangesAsync so
    /// the history lands in the same database transaction as the appointment change.
    /// Call BEFORE domain mutations that clear operative data (e.g. Cancel, ReleaseHold).
    /// </summary>
    private void StageHistory(
        Appointment appointment, string accion, Guid actorUserId, string? motivo,
        Guid? patientId, Guid? tandaId)
    {
        blockHistoryRepository.AddRange([new BlockHistory(new BlockHistoryCreateParams(
            Guid.NewGuid(), appointment.Fecha, appointment.Hora, appointment.CameraId,
            appointment.Id, appointment.Lugar, accion, patientId, actorUserId, motivo,
            appointment.ReferidoTercero, appointment.ModalidadCobro, appointment.ObraSocialId,
            appointment.NumeroAutorizacion, null, null, appointment.MedicoId,
            appointment.EsNuevoIngreso, appointment.ReferenteId, tandaId,
            appointment.SesionesAutorizadas, appointment.CicloObraSocialId))]);
    }

    /// <summary>
    /// Stages a single summary history entry for a block operation (SlotId/Lugar = null).
    /// Call BEFORE domain mutations that clear operative data.
    /// </summary>
    private void StageBlockHistory(
        IReadOnlyCollection<Appointment> slots,
        Guid? patientId, Guid? tandaId,
        string accion, Guid actorUserId, string? motivo)
    {
        if (slots.Count == 0) return;
        var first = slots.First();
        blockHistoryRepository.AddRange([new BlockHistory(new BlockHistoryCreateParams(
            Guid.NewGuid(), first.Fecha, first.Hora, first.CameraId,
            null, null, accion, patientId, actorUserId, motivo,
            first.ReferidoTercero, first.ModalidadCobro, first.ObraSocialId,
            first.NumeroAutorizacion, null, null, first.MedicoId,
            first.EsNuevoIngreso, first.ReferenteId, tandaId,
            first.SesionesAutorizadas, first.CicloObraSocialId))]);
    }

    /// <summary>
    /// Stages one history entry per item in the collection.
    /// Call BEFORE domain mutations that clear operative data.
    /// </summary>
    private void StageHistoryRange(
        IReadOnlyCollection<(Appointment Appt, Guid? PatientId, Guid? TandaId)> items,
        string accion, Guid actorUserId, string? motivo)
    {
        if (items.Count == 0) return;
        blockHistoryRepository.AddRange(items.Select(x => new BlockHistory(new BlockHistoryCreateParams(
            Guid.NewGuid(), x.Appt.Fecha, x.Appt.Hora, x.Appt.CameraId,
            x.Appt.Id, x.Appt.Lugar, accion, x.PatientId, actorUserId, motivo,
            x.Appt.ReferidoTercero, x.Appt.ModalidadCobro, x.Appt.ObraSocialId,
            x.Appt.NumeroAutorizacion, null, null, x.Appt.MedicoId,
            x.Appt.EsNuevoIngreso, x.Appt.ReferenteId, x.TandaId,
            x.Appt.SesionesAutorizadas, x.Appt.CicloObraSocialId))).ToArray());
    }

    private static Guid DeterministicGuid(string seed, string suffix)
    {
        Span<byte> hash = stackalloc byte[16];
        MD5.HashData(Encoding.UTF8.GetBytes($"{seed}:{suffix}"), hash);
        return new Guid(hash);
    }

    public async Task<int> GenerateAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        if (fecha == default)
        {
            throw new ValidationException("Fecha invalida.");
        }

        var hours = (await scheduleHourRepository.GetAsync(cancellationToken))
            .Where(x => x.Activo)
            .OrderBy(x => x.Orden)
            .ToArray();

        var cameras = (await cameraRepository.GetAsync(cancellationToken))
            .Where(x => x.Activa)
            .OrderBy(x => x.Id)
            .ToArray();

        var existing = await appointmentRepository.GetByDateAsync(fecha, cancellationToken);
        var existingKeys = existing
            .Select(x => (x.Hora, x.CameraId, x.Lugar))
            .ToHashSet();

        var inserted = 0;

        foreach (var hour in hours)
        {
            if (!TimeOnly.TryParse(hour.Hora, CultureInfo.InvariantCulture, DateTimeStyles.None, out var horaTime))
            {
                throw new ValidationException($"Horario invalido en configuración: '{hour.Hora}'.");
            }

            foreach (var camera in cameras)
            {
                for (var lugar = 0; lugar < camera.Capacidad; lugar++)
                {
                    if (existingKeys.Contains((horaTime, (int?)camera.Id, lugar)))
                    {
                        continue;
                    }

                    var schedule = new Schedule(Guid.NewGuid(), fecha, horaTime, lugar, $"camara-{camera.Id}");
                    await scheduleRepository.AddAsync(schedule, cancellationToken);

                    var appointment = new Appointment(Guid.NewGuid(), schedule.Id, fecha, horaTime, lugar, camera.Id);
                    await appointmentRepository.AddAsync(appointment, cancellationToken);

                    inserted++;
                }
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return inserted;
    }

public async Task<int> RepairRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken)
    {
        if (fechaFin < fechaInicio)
        {
            throw new ValidationException("Rango invalido.");
        }

        var total = 0;
        for (var fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
        {
            total += await GenerateAsync(fecha, cancellationToken);
        }

        return total;
    }
}

public sealed record AppointmentsDataAccessDependencies(
    IAppointmentRepository AppointmentRepository,
    IScheduleRepository ScheduleRepository,
    IUserRepository UserRepository,
    IScheduleHourRepository ScheduleHourRepository,
    ICameraRepository CameraRepository,
    IPatientRepository PatientRepository,
    IMedicoRepository MedicoRepository,
    IReferenteRepository ReferenteRepository,
    IObraSocialRepository ObraSocialRepository,
    IBlockHistoryRepository BlockHistoryRepository);

public sealed record AppointmentsRuntimeDependencies(
    IWhatsappService WhatsappService,
    IUnitOfWork UnitOfWork,
    IIdempotencyStore IdempotencyStore,
    IClock Clock);
