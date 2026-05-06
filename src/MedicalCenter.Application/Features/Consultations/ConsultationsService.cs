using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Mappings;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using DomainClinicalHistory = MedicalCenter.Domain.Entities.ClinicalHistory;

namespace MedicalCenter.Application.Features.Consultations;

public sealed class ConsultationsService(
    ConsultationsDataAccessDependencies dataAccess,
    ConsultationsRuntimeDependencies runtime) : IConsultationsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private const string HorarioNoEncontradoMessage = "Horario no encontrado.";
    private readonly IConsultationRepository consultationRepository = dataAccess.ConsultationRepository;
    private readonly IUserRepository userRepository = dataAccess.UserRepository;
    private readonly IPatientRepository patientRepository = dataAccess.PatientRepository;
    private readonly IMedicoRepository medicoRepository = dataAccess.MedicoRepository;
    private readonly IClinicalHistoryRepository clinicalHistoryRepository = dataAccess.ClinicalHistoryRepository;
    private readonly IUnitOfWork unitOfWork = dataAccess.UnitOfWork;
    private readonly IIdempotencyStore idempotencyStore = dataAccess.IdempotencyStore;
    private readonly IClock clock = runtime.Clock;

    public async Task<IReadOnlyCollection<ConsultationScheduleHourSummary>> GetScheduleHoursAsync(CancellationToken cancellationToken) =>
        (await consultationRepository.GetScheduleHoursAsync(cancellationToken))
            .Select(x => x.ToSummary())
            .ToArray();

    public async Task<ConsultationScheduleHourSummary> CreateScheduleHourAsync(ConsultationScheduleHourUpsertCommand command, CancellationToken cancellationToken)
    {
        ValidateHora(command.Hora);
        var entity = new ConsultationScheduleHour(await consultationRepository.GetNextScheduleHourIdAsync(cancellationToken), command.Hora.Trim(), command.Orden, true);
        await consultationRepository.AddScheduleHourAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.ToSummary();
    }

    public async Task<ConsultationScheduleHourSummary> UpdateScheduleHourAsync(int id, ConsultationScheduleHourUpsertCommand command, CancellationToken cancellationToken)
    {
        ValidateHora(command.Hora);
        var entity = await consultationRepository.GetScheduleHourByIdAsync(id, cancellationToken) ?? throw new NotFoundException(HorarioNoEncontradoMessage);
        entity.Update(command.Hora.Trim(), command.Orden);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.ToSummary();
    }

    public async Task<ConsultationScheduleHourSummary> ToggleScheduleHourAsync(int id, bool activo, CancellationToken cancellationToken)
    {
        var entity = await consultationRepository.GetScheduleHourByIdAsync(id, cancellationToken) ?? throw new NotFoundException(HorarioNoEncontradoMessage);
        entity.SetActivo(activo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.ToSummary();
    }

    public async Task<ConsultationScheduleHourDeletionPreviewSummary> PreviewDeleteScheduleHourAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await consultationRepository.GetScheduleHourByIdAsync(id, cancellationToken) ?? throw new NotFoundException(HorarioNoEncontradoMessage);
        var futureSlots = await consultationRepository.CountFutureSlotsByHourAsync(ParseHour(entity.Hora), DateOnly.FromDateTime(clock.UtcNow.UtcDateTime.Date), cancellationToken);
        return new ConsultationScheduleHourDeletionPreviewSummary(entity.Id, entity.Hora, futureSlots == 0, futureSlots);
    }

    public async Task<ConsultationScheduleHourSummary?> DeleteScheduleHourAsync(int id, CancellationToken cancellationToken)
    {
        var preview = await PreviewDeleteScheduleHourAsync(id, cancellationToken);
        if (!preview.CanDelete)
        {
            throw new ConflictException("No se puede desactivar el horario porque tiene consultas futuras.");
        }

        var entity = await consultationRepository.GetScheduleHourByIdAsync(id, cancellationToken) ?? throw new NotFoundException(HorarioNoEncontradoMessage);
        entity.SetActivo(false);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return entity.ToSummary();
    }

    public async Task<int> GenerateAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var activeHours = await consultationRepository.GetScheduleHoursAsync(cancellationToken);
        var hours = activeHours.Where(x => x.Activo).OrderBy(x => x.Orden).ToArray();
        var existing = await consultationRepository.GetByDateAsync(fecha, cancellationToken);
        var existingHours = existing.Select(x => x.Hora).ToHashSet();
        var inserted = 0;

        foreach (var hour in hours)
        {
            var parsedHour = ParseHour(hour.Hora);
            if (existingHours.Contains(parsedHour))
            {
                continue;
            }

            await consultationRepository.AddAsync(new ConsultationSlot(Guid.NewGuid(), fecha, parsedHour), cancellationToken);
            inserted++;
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

    public async Task<IReadOnlyCollection<ConsultationSlotSummary>> GetByDateAsync(DateOnly? fecha, CancellationToken cancellationToken)
    {
        if (fecha is null)
        {
            return [];
        }

        return await MapSlotsAsync(await consultationRepository.GetByDateAsync(fecha.Value, cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConsultationSlotSummary>> GetByRangeAsync(DateOnly fechaInicio, DateOnly fechaFin, CancellationToken cancellationToken) =>
        await MapSlotsAsync(await consultationRepository.GetByRangeAsync(fechaInicio, fechaFin, cancellationToken), cancellationToken);

    public Task<ConsultationSlotSummary> AssignAsync(Guid actorUserId, Guid slotId, string idempotencyKey, AssignConsultationCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);
        return ExecuteIdempotentAsync(
            $"consultas.asignaciones:{slotId}",
            idempotencyKey,
            command,
            async () =>
            {
                await RequireActorAsync(actorUserId, "consultas.asignar", cancellationToken);
                await EnsureActivePatientAsync(command.PacienteId, cancellationToken);
                await ValidateMedicoAssignmentAsync(command, cancellationToken);
                var slot = await GetAssignableSlotAsync(slotId, command.PacienteId, cancellationToken);
                AssignSlot(slot, command, actorUserId);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return await MapAsync(slot, cancellationToken);
            },
            cancellationToken);
    }

    public Task<ConsultationSlotSummary> CancelAsync(Guid actorUserId, Guid slotId, string idempotencyKey, CancelConsultationCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);
        return ExecuteIdempotentAsync(
            $"consultas.cancelaciones:{slotId}",
            idempotencyKey,
            command,
            async () =>
            {
                await RequireActorAsync(actorUserId, "consultas.cancelar", cancellationToken);
                var slot = await consultationRepository.GetByIdAsync(slotId, cancellationToken) ?? throw new NotFoundException("Consulta no encontrada.");

                try
                {
                    slot.Cancel(command.Motivo, clock.UtcNow);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ConflictException(exception.Message);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return await MapAsync(slot, cancellationToken);
            },
            cancellationToken);
    }

    public Task<ConsultationSlotSummary> RescheduleAsync(Guid actorUserId, Guid slotId, string idempotencyKey, RescheduleConsultationCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);
        return ExecuteIdempotentAsync(
            $"consultas.reprogramaciones:{slotId}",
            idempotencyKey,
            command,
            async () =>
            {
                await RequireActorAsync(actorUserId, "consultas.reprogramar", cancellationToken);
                var source = await consultationRepository.GetByIdAsync(slotId, cancellationToken) ?? throw new NotFoundException("Consulta origen no encontrada.");
                var target = await consultationRepository.GetByIdAsync(command.TargetSlotId, cancellationToken) ?? throw new NotFoundException("Consulta destino no encontrada.");

                EnsureNotPast(source);
                EnsureNotPast(target);

                if (source.Estado is not (ConsultationStatus.Confirmada or ConsultationStatus.Completada))
                {
                    throw new ConflictException("Consulta origen invalida.");
                }

                if (!target.IsReservable())
                {
                    throw new ConflictException("Consulta destino invalida.");
                }

                var patientId = source.PacienteId ?? throw new ConflictException("Consulta origen invalida.");
                await EnsurePatientHasNoConsecutiveConsultationsAsync(patientId, target.Fecha, target.Hora, source.Id, cancellationToken);

                var medicoUserId = command.MedicoUserId ?? source.MedicoUserId;
                var medicoId = command.MedicoId ?? (medicoUserId.HasValue ? null : source.MedicoId);
                if (medicoUserId is null && medicoId is null)
                    throw new ConflictException("Médico requerido.");

                try
                {
                    source.RescheduleToFreeSlot();
                    target.Assign(patientId, medicoId, null, actorUserId, clock.UtcNow, medicoUserId);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ConflictException(exception.Message);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return await MapAsync(target, cancellationToken);
            },
            cancellationToken);
    }

    public Task<ConsultationSlotSummary> CloseAsync(Guid actorUserId, Guid slotId, string idempotencyKey, CloseConsultationCommand command, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);
        return ExecuteIdempotentAsync(
            $"consultas.cierres:{slotId}",
            idempotencyKey,
            command,
            async () =>
            {
                var actor = await RequireActorAsync(actorUserId, "consultas.cerrar", cancellationToken);
                var slot = await consultationRepository.GetByIdAsync(slotId, cancellationToken) ?? throw new NotFoundException("Consulta no encontrada.");

                try
                {
                    slot.Close(command.Estado, actorUserId, clock.UtcNow);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ConflictException(exception.Message);
                }

                if (slot.Estado == ConsultationStatus.Completada && !string.IsNullOrWhiteSpace(command.Nota) && slot.PacienteId.HasValue)
                {
                    await EnsureClinicalHistoryAsync(slot.PacienteId.Value, cancellationToken);
                    var evolution = new ClinicalEvolution(new ClinicalEvolutionCreateData
                    {
                        Id = Guid.NewGuid(),
                        PatientId = slot.PacienteId.Value,
                        ConsultaSlotId = slot.Id,
                        MedicoId = slot.MedicoId ?? 0,
                        MedicoUserId = slot.MedicoUserId,
                        AuthorProfileId = actor.Id,
                        FechaClinica = slot.Fecha,
                        Titulo = command.Titulo,
                        Nota = command.Nota.Trim(),
                        DiagnosticoImpresion = command.DiagnosticoImpresion,
                        Indicaciones = command.Indicaciones
                    });
                    await clinicalHistoryRepository.AddEvolutionAsync(evolution, cancellationToken);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return await MapAsync(slot, cancellationToken);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<ConsultationSessionSummary>> GetCompletedSessionsAsync(Guid patientId, CancellationToken cancellationToken) =>
        (await consultationRepository.GetSessionsByPatientIdAsync(patientId, cancellationToken)).Select(x => x.ToSummary()).ToArray();

    private static ConsultationSlotSummary Map(ConsultationSlot slot, GuidLookupSummary? patient, IntLookupSummary? medico, GuidLookupSummary? medicoUser, GuidLookupSummary? confirmadoPor, GuidLookupSummary? cerradoPor) =>
        new(slot.Id, slot.Fecha, slot.Hora, slot.Estado.ToString().ToLowerInvariant(), slot.PacienteId, slot.MedicoId, slot.MedicoUserId, slot.MotivoCancelacion, slot.ObservacionesAdmin, slot.ConfirmadoPor, slot.ConfirmadoAt, slot.CerradoPor, slot.CerradoAt, slot.CreatedAt, slot.UpdatedAt, patient, medico, medicoUser, confirmadoPor, cerradoPor);

    private async Task<ConsultationSlotSummary> MapAsync(ConsultationSlot slot, CancellationToken cancellationToken)
    {
        var patient = slot.PacienteId.HasValue ? await patientRepository.GetByIdAsync(slot.PacienteId.Value, cancellationToken) : null;
        var medico = (slot.MedicoId.HasValue && !slot.MedicoUserId.HasValue) ? await medicoRepository.GetByIdAsync(slot.MedicoId.Value, cancellationToken) : null;
        var medicoUser = slot.MedicoUserId.HasValue ? await userRepository.GetByIdAsync(slot.MedicoUserId.Value, cancellationToken) : null;
        var confirmedBy = slot.ConfirmadoPor.HasValue ? await userRepository.GetByIdAsync(slot.ConfirmadoPor.Value, cancellationToken) : null;
        var closedBy = slot.CerradoPor.HasValue ? await userRepository.GetByIdAsync(slot.CerradoPor.Value, cancellationToken) : null;

        return Map(
            slot,
            patient is null ? null : new GuidLookupSummary(patient.Id, patient.Nombre, patient.DocumentoIdentidad, patient.Email, patient.IsActive),
            medico is null ? null : new IntLookupSummary(medico.Id, medico.Nombre, null, medico.Activo),
            medicoUser is null ? null : new GuidLookupSummary(medicoUser.Id, medicoUser.Nombre ?? medicoUser.Identifier, null, medicoUser.Email, medicoUser.IsActive),
            confirmedBy is null ? null : new GuidLookupSummary(confirmedBy.Id, confirmedBy.Nombre ?? confirmedBy.Identifier, null, confirmedBy.Email, confirmedBy.IsActive),
            closedBy is null ? null : new GuidLookupSummary(closedBy.Id, closedBy.Nombre ?? closedBy.Identifier, null, closedBy.Email, closedBy.IsActive));
    }

    private async Task<IReadOnlyCollection<ConsultationSlotSummary>> MapSlotsAsync(IEnumerable<ConsultationSlot> slots, CancellationToken cancellationToken)
    {
        var result = new List<ConsultationSlotSummary>();
        foreach (var slot in slots)
        {
            result.Add(await MapAsync(slot, cancellationToken));
        }

        return result;
    }

    private async Task EnsurePatientHasNoConsecutiveConsultationsAsync(Guid patientId, DateOnly fecha, TimeOnly hora, Guid? ignoreSlotId, CancellationToken cancellationToken)
    {
        var slots = await consultationRepository.GetByDateAsync(fecha, cancellationToken);
        if (slots.Any(x => x.PacienteId == patientId && x.Id != ignoreSlotId && x.Estado == ConsultationStatus.Confirmada && Math.Abs(ToMinutes(x.Hora) - ToMinutes(hora)) <= 60))
        {
            throw new ConflictException("No puedes reservar consultas consecutivas en el mismo dia.");
        }
    }

    private void EnsureNotPast(ConsultationSlot slot)
    {
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime.Date);
        if (slot.Fecha < today || (slot.Fecha == today && slot.Hora <= TimeOnly.FromDateTime(now.UtcDateTime)))
        {
            throw new ConflictException("No se pueden operar consultas pasadas.");
        }
    }

    private async Task EnsureClinicalHistoryAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var history = await clinicalHistoryRepository.GetByPatientIdAsync(patientId, cancellationToken);
        if (history is null)
        {
            await clinicalHistoryRepository.AddAsync(new DomainClinicalHistory(new ClinicalHistoryCreateParams(patientId, 1, null, null, null, null)), cancellationToken);
        }
    }

    private async Task<User> RequireActorAsync(Guid actorUserId, string permission, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken) ?? throw new UnauthorizedException();
        if (!actor.IsStaff || !actor.HasPermission(permission))
        {
            throw new ForbiddenException("Prohibido");
        }

        return actor;
    }

    private static void ValidateHora(string hora)
    {
        if (!TimeOnly.TryParse(hora, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            throw new ValidationException("Hora invalida.");
        }
    }

    private static TimeOnly ParseHour(string hora) => TimeOnly.Parse(hora, CultureInfo.InvariantCulture, DateTimeStyles.None);

    private async Task EnsureActivePatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(patientId, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado.");
        if (!patient.IsActive)
        {
            throw new ConflictException("El paciente no se encuentra activo.");
        }
    }

    private async Task ValidateMedicoAssignmentAsync(AssignConsultationCommand command, CancellationToken cancellationToken)
    {
        if (command.MedicoUserId.HasValue)
        {
            var medicoUser = await userRepository.GetByIdAsync(command.MedicoUserId.Value, cancellationToken) ?? throw new NotFoundException("Medico no encontrado.");
            if (!medicoUser.IsActive)
            {
                throw new ConflictException("El Medico no se encuentra activo.");
            }

            if (!medicoUser.Roles.Any(role => role.Code == "medico"))
            {
                throw new ConflictException("El usuario no tiene rol de Medico.");
            }

            return;
        }

        if (command.MedicoId.HasValue)
        {
            var medico = await medicoRepository.GetByIdAsync(command.MedicoId.Value, cancellationToken) ?? throw new NotFoundException("Medico no encontrado.");
            if (!medico.Activo)
            {
                throw new ConflictException("El Medico no se encuentra activo.");
            }

            return;
        }

        throw new ValidationException("Medico requerido.");
    }

    private async Task<ConsultationSlot> GetAssignableSlotAsync(Guid slotId, Guid patientId, CancellationToken cancellationToken)
    {
        var slot = await consultationRepository.GetByIdAsync(slotId, cancellationToken) ?? throw new NotFoundException("Consulta no encontrada.");
        EnsureNotPast(slot);
        await EnsurePatientHasNoConsecutiveConsultationsAsync(patientId, slot.Fecha, slot.Hora, null, cancellationToken);
        return slot;
    }

    private void AssignSlot(ConsultationSlot slot, AssignConsultationCommand command, Guid actorUserId)
    {
        try
        {
            slot.Assign(command.PacienteId, command.MedicoId, command.ObservacionesAdmin, actorUserId, clock.UtcNow, command.MedicoUserId);
        }
        catch (InvalidOperationException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }

    private static void ValidateRequiredIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ValidationException("El header Idempotency-Key es obligatorio para esta operacion.");
        }
    }

    private async Task<ConsultationSlotSummary> ExecuteIdempotentAsync<TPayload>(
        string operation,
        string idempotencyKey,
        TPayload payload,
        Func<Task<ConsultationSlotSummary>> action,
        CancellationToken cancellationToken)
    {
        var requestHash = ComputeHash(payload);
        var reservation = await idempotencyStore.ReserveAsync(operation, idempotencyKey, requestHash, cancellationToken);
        return reservation.State switch
        {
            IdempotencyReservationState.Completed => JsonSerializer.Deserialize<ConsultationSlotSummary>(reservation.ResponsePayload ?? string.Empty, SerializerOptions) ?? throw new ConflictException("No se pudo reconstruir la respuesta idempotente."),
            IdempotencyReservationState.Pending => throw new ConflictException("La operacion aun esta en proceso.", "operation_pending"),
            IdempotencyReservationState.Mismatch => throw new ConflictException("La Idempotency-Key ya fue usada con otro payload.", "idempotency_mismatch"),
            IdempotencyReservationState.Acquired => await CompleteIdempotentAsync(operation, idempotencyKey, action, cancellationToken),
            _ => throw new ConflictException("Estado de idempotencia invalido.")
        };
    }

    private async Task<ConsultationSlotSummary> CompleteIdempotentAsync(
        string operation,
        string idempotencyKey,
        Func<Task<ConsultationSlotSummary>> action,
        CancellationToken cancellationToken)
    {
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
    }

    private static string ComputeHash<TPayload>(TPayload payload)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static int ToMinutes(TimeOnly hora) => hora.Hour * 60 + hora.Minute;
}

public sealed record ConsultationsDataAccessDependencies(
    IConsultationRepository ConsultationRepository,
    IUserRepository UserRepository,
    IPatientRepository PatientRepository,
    IMedicoRepository MedicoRepository,
    IClinicalHistoryRepository ClinicalHistoryRepository,
    IUnitOfWork UnitOfWork,
    IIdempotencyStore IdempotencyStore);

public sealed record ConsultationsRuntimeDependencies(IClock Clock);
