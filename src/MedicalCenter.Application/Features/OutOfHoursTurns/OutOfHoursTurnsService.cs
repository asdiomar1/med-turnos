using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.OutOfHoursTurns;

public sealed class OutOfHoursTurnsService(
    IOutOfHoursTurnRepository outOfHoursTurnRepository,
    IUserRepository userRepository,
    IPatientRepository patientRepository,
    IMedicoRepository medicoRepository,
    IUnitOfWork unitOfWork,
    IIdempotencyStore idempotencyStore) : IOutOfHoursTurnsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyCollection<OutOfHoursTurnSummary>> GetByDateAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var turns = await outOfHoursTurnRepository.GetByDateAsync(fecha, cancellationToken);
        var result = new List<OutOfHoursTurnSummary>();
        foreach (var turn in turns)
        {
            result.Add(await MapAsync(turn, cancellationToken));
        }

        return result;
    }

    public Task<OutOfHoursTurnSummary> CreateAsync(Guid actorUserId, OutOfHoursTurnCreateCommand command, string idempotencyKey, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);
        var hora = command.Hora.ToString("HH:mm");
        return ExecuteIdempotentAsync(
            $"turnos.fuera_horario.creaciones:{command.Fecha:yyyy-MM-dd}:{hora}:{command.PacienteId}",
            idempotencyKey,
            command,
            async () =>
            {
                var actor = await RequireActorAsync(actorUserId, cancellationToken);
                var patient = await patientRepository.GetByIdAsync(command.PacienteId, cancellationToken) ?? throw new NotFoundException("Paciente no encontrado.");
                if (!patient.IsActive)
                {
                    throw new ConflictException("El paciente no se encuentra activo.");
                }

                var existing = await outOfHoursTurnRepository.GetByDateAsync(command.Fecha, cancellationToken);
                if (existing.Any(x => x.Hora == command.Hora && x.PacienteId == command.PacienteId))
                {
                    throw new ConflictException("Ya existe un turno fuera de horario para ese paciente y horario.");
                }

                var operadorCamaraId = command.OperadorCamaraId ?? actor.Id;
                var turno = new OutOfHoursTurn(new OutOfHoursTurnCreateParams(
                    Guid.NewGuid(),
                    command.Fecha,
                    command.Hora,
                    command.PacienteId,
                    actor.Id,
                    operadorCamaraId,
                    command.Notas,
                    command.EsMonoxido,
                    command.MonoxidoOrdenMedica,
                    command.MonoxidoResumenClinico,
                    command.MonoxidoMedicoId,
                    command.MonoxidoMedicoUserId));

                await outOfHoursTurnRepository.AddAsync(turno, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return await MapAsync(turno, cancellationToken);
            },
            cancellationToken);
    }

    public Task<OutOfHoursTurnSummary> CancelAsync(Guid actorUserId, Guid turnoId, string idempotencyKey, CancellationToken cancellationToken)
    {
        ValidateRequiredIdempotencyKey(idempotencyKey);
        return ExecuteIdempotentAsync(
            $"turnos.fuera_horario.cancelaciones:{turnoId}",
            idempotencyKey,
            new { turnoId },
            async () =>
            {
                var actor = await RequireActorAsync(actorUserId, cancellationToken);
                _ = actor;
                var turno = await outOfHoursTurnRepository.GetByIdAsync(turnoId, cancellationToken) ?? throw new NotFoundException("Turno fuera de horario no encontrado.");
                await outOfHoursTurnRepository.DeleteAsync(turno, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return await MapAsync(turno, cancellationToken);
            },
            cancellationToken);
    }

    private async Task<OutOfHoursTurnSummary> MapAsync(OutOfHoursTurn turno, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByIdAsync(turno.PacienteId, cancellationToken);
        var medicoLegacy = (turno.MonoxidoMedicoId.HasValue && !turno.MonoxidoMedicoUserId.HasValue)
            ? await medicoRepository.GetByIdAsync(turno.MonoxidoMedicoId.Value, cancellationToken)
            : null;
        var medicoUser = turno.MonoxidoMedicoUserId.HasValue
            ? await userRepository.GetByIdAsync(turno.MonoxidoMedicoUserId.Value, cancellationToken)
            : null;
        var operador = await userRepository.GetByIdAsync(turno.OperadorCamaraId, cancellationToken);

        return new OutOfHoursTurnSummary(
            turno.Id,
            turno.Fecha,
            turno.Hora,
            turno.PacienteId,
            turno.Notas,
            turno.CreadoPor,
            turno.OperadorCamaraId,
            turno.CreatedAt,
            turno.EsMonoxido,
            turno.MonoxidoOrdenMedica,
            turno.MonoxidoResumenClinico,
            turno.MonoxidoMedicoId,
            turno.MonoxidoMedicoUserId,
            patient is null ? null : new GuidLookupSummary(patient.Id, patient.Nombre, patient.DocumentoIdentidad, patient.Email, patient.IsActive),
            medicoLegacy is null ? null : new IntLookupSummary(medicoLegacy.Id, medicoLegacy.Nombre, null, medicoLegacy.Activo),
            medicoUser is null ? null : new GuidLookupSummary(medicoUser.Id, medicoUser.Nombre ?? medicoUser.Identifier, null, medicoUser.Email, medicoUser.IsActive),
            operador is null ? null : new GuidLookupSummary(operador.Id, operador.Nombre ?? operador.Identifier, null, operador.Email, operador.IsActive));
    }

    private async Task<User> RequireActorAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken) ?? throw new UnauthorizedException();
        if (!actor.IsStaff || !actor.HasPermission("turnos.fuera_horario"))
        {
            throw new ForbiddenException("Prohibido");
        }

        return actor;
    }

    private async Task<OutOfHoursTurnSummary> ExecuteIdempotentAsync<TPayload>(
        string operation,
        string idempotencyKey,
        TPayload payload,
        Func<Task<OutOfHoursTurnSummary>> action,
        CancellationToken cancellationToken)
    {
        var requestHash = ComputeHash(payload);
        var reservation = await idempotencyStore.ReserveAsync(operation, idempotencyKey, requestHash, cancellationToken);
        return reservation.State switch
        {
            IdempotencyReservationState.Completed => JsonSerializer.Deserialize<OutOfHoursTurnSummary>(reservation.ResponsePayload ?? string.Empty, SerializerOptions) ?? throw new ConflictException("No se pudo reconstruir la respuesta idempotente."),
            IdempotencyReservationState.Pending => throw new ConflictException("La operacion aun esta en proceso.", "operation_pending"),
            IdempotencyReservationState.Mismatch => throw new ConflictException("La Idempotency-Key ya fue usada con otro payload.", "idempotency_mismatch"),
            IdempotencyReservationState.Acquired => await CompleteIdempotentAsync(operation, idempotencyKey, action, cancellationToken),
            _ => throw new ConflictException("Estado de idempotencia invalido.")
        };
    }

    private async Task<OutOfHoursTurnSummary> CompleteIdempotentAsync(
        string operation,
        string idempotencyKey,
        Func<Task<OutOfHoursTurnSummary>> action,
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
}
