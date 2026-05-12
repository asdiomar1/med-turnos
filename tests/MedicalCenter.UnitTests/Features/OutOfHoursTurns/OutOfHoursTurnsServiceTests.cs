using System.Text.Json;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.OutOfHoursTurns;
using MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.OutOfHoursTurns;

// Scenario map:
// - GetByDateAsync: empty result, legacy-medico enrichment, medico-user enrichment
// - CreateAsync: unauthorized, forbidden, missing key, completed/pending/mismatch/acquired, duplicate, success
// - CancelAsync: unauthorized, forbidden, completed/pending/mismatch, not-found, success
public sealed class OutOfHoursTurnsServiceTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutOfHoursTurnRepository outOfHoursTurnRepository = Substitute.For<IOutOfHoursTurnRepository>();
    private readonly IUserRepository userRepository = Substitute.For<IUserRepository>();
    private readonly IPatientRepository patientRepository = Substitute.For<IPatientRepository>();
    private readonly IMedicoRepository medicoRepository = Substitute.For<IMedicoRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIdempotencyStore idempotencyStore = Substitute.For<IIdempotencyStore>();

    private OutOfHoursTurnsService Sut => new(
        outOfHoursTurnRepository,
        userRepository,
        patientRepository,
        medicoRepository,
        unitOfWork,
        idempotencyStore);

    [Fact]
    public async Task GetByDateAsync_WhenNoTurns_ReturnsEmptyCollection()
    {
        var fecha = new DateOnly(2026, 5, 2);
        outOfHoursTurnRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns([]);

        var result = await Sut.GetByDateAsync(fecha, CancellationToken.None);

        Assert.Empty(result);
        await patientRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await userRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await medicoRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task GetByDateAsync_WithLegacyMedico_ReturnsEnrichedSummary()
    {
        var fecha = new DateOnly(2026, 5, 2);
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var medicoId = 42;
        var turno = CreateTurn(
            Guid.NewGuid(),
            fecha,
            new TimeOnly(9, 15),
            patientId,
            actorId,
            actorId,
            "Primera consulta",
            esMonoxido: true,
            monoxidoOrdenMedica: true,
            monoxidoResumenClinico: false,
            monoxidoMedicoId: medicoId,
            monoxidoMedicoUserId: null);
        var patient = CreatePatient(patientId, "Paciente Uno");
        var medico = CreateMedico(medicoId, "Dr. Legacy");
        var operador = CreateActor(actorId, permission: null, nombre: "Operador Cámara");

        outOfHoursTurnRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns([turno]);
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        medicoRepository.GetByIdAsync(medicoId, Arg.Any<CancellationToken>()).Returns(medico);
        userRepository.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(operador);

        var result = await Sut.GetByDateAsync(fecha, CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.Equal(turno.Id, summary.Id);
        Assert.Equal(turno.Fecha, summary.Fecha);
        Assert.Equal(turno.Hora, summary.Hora);
        Assert.Equal(turno.Notas, summary.Notas);
        Assert.Equal(turno.CreadoPor, summary.CreadoPor);
        Assert.Equal(turno.OperadorCamaraId, summary.OperadorCamaraId);
        Assert.True(summary.EsMonoxido);
        Assert.True(summary.MonoxidoOrdenMedica);
        Assert.False(summary.MonoxidoResumenClinico);
        Assert.Equal(patientId, summary.Paciente!.Id);
        Assert.Equal("Paciente Uno", summary.Paciente.Nombre);
        Assert.Equal(0, summary.MonoxidoMedico!.Id);
        Assert.Equal("Dr. Legacy", summary.MonoxidoMedico.Nombre);
        Assert.Equal(actorId, summary.OperadorCamara!.Id);
        Assert.Equal("Operador Cámara", summary.OperadorCamara.Nombre);
        await patientRepository.Received(1).GetByIdAsync(patientId, Arg.Any<CancellationToken>());
        await medicoRepository.Received(1).GetByIdAsync(medicoId, Arg.Any<CancellationToken>());
        await userRepository.Received(1).GetByIdAsync(actorId, Arg.Any<CancellationToken>());
        await userRepository.DidNotReceive().GetByIdAsync(Arg.Is<Guid>(x => x != actorId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByDateAsync_WithMedicoUserAndMissingPatient_ReturnsEnrichedSummary()
    {
        var fecha = new DateOnly(2026, 5, 2);
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var medicoUserId = Guid.NewGuid();
        var turno = CreateTurn(
            Guid.NewGuid(),
            fecha,
            new TimeOnly(10, 45),
            patientId,
            actorId,
            actorId,
            null,
            esMonoxido: false,
            monoxidoOrdenMedica: false,
            monoxidoResumenClinico: false,
            monoxidoMedicoId: null,
            monoxidoMedicoUserId: medicoUserId);
        var medicoUser = CreateActor(medicoUserId, permission: null, nombre: "Dr. Usuario");
        var operador = CreateActor(actorId, permission: null, nombre: "Operador Cámara");

        outOfHoursTurnRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>()).Returns([turno]);
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        userRepository.GetByIdAsync(medicoUserId, Arg.Any<CancellationToken>()).Returns(medicoUser);
        userRepository.GetByIdAsync(actorId, Arg.Any<CancellationToken>()).Returns(operador);

        var result = await Sut.GetByDateAsync(fecha, CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.Null(summary.Paciente);
        Assert.Null(summary.MonoxidoMedico);
        Assert.Equal(medicoUserId, summary.MonoxidoMedicoUser!.Id);
        Assert.Equal("Dr. Usuario", summary.MonoxidoMedicoUser.Nombre);
        Assert.Equal(actorId, summary.OperadorCamara!.Id);
        Assert.Equal("Operador Cámara", summary.OperadorCamara.Nombre);
        await patientRepository.Received(1).GetByIdAsync(patientId, Arg.Any<CancellationToken>());
        await userRepository.Received(1).GetByIdAsync(medicoUserId, Arg.Any<CancellationToken>());
        await userRepository.Received(1).GetByIdAsync(actorId, Arg.Any<CancellationToken>());
        await medicoRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task CreateAsync_WhenActorMissing_ThrowsUnauthorizedExceptionAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 5, 3);
        var hora = new TimeOnly(11, 0);
        var idempotencyKey = "create-unauthorized";
        var command = CreateCommand(fecha, hora, patientId, operadorCamaraId: null);

        SetupAcquired(CreateOperation(command), idempotencyKey);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns((User?)null);
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(CreatePatient(patientId, "Paciente Uno"));

        await Assert.ThrowsAsync<UnauthorizedException>(() => Sut.CreateAsync(actorUserId, command, idempotencyKey, CancellationToken.None));

        await idempotencyStore.Received(1).FailAsync(CreateOperation(command), idempotencyKey, Arg.Any<CancellationToken>());
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenActorForbidden_ThrowsForbiddenExceptionAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var command = CreateCommand(new DateOnly(2026, 5, 3), new TimeOnly(11, 30), patientId, operadorCamaraId: null);
        var idempotencyKey = "create-forbidden";

        SetupAcquired(CreateOperation(command), idempotencyKey);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(CreateActor(actorUserId, permission: null));
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(CreatePatient(patientId, "Paciente Uno"));

        await Assert.ThrowsAsync<ForbiddenException>(() => Sut.CreateAsync(actorUserId, command, idempotencyKey, CancellationToken.None));

        await idempotencyStore.Received(1).FailAsync(CreateOperation(command), idempotencyKey, Arg.Any<CancellationToken>());
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenIdempotencyKeyMissing_ThrowsValidationException()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var command = CreateCommand(new DateOnly(2026, 5, 3), new TimeOnly(12, 0), patientId, operadorCamaraId: null);

        await Assert.ThrowsAsync<ValidationException>(() => Sut.CreateAsync(actorUserId, command, " ", CancellationToken.None));

        await idempotencyStore.DidNotReceiveWithAnyArgs().ReserveAsync(default!, default!, default!, default);
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenIdempotencyCompleted_ReturnsStoredResponse()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var command = CreateCommand(new DateOnly(2026, 5, 3), new TimeOnly(13, 0), patientId, operadorCamaraId: null);
        var idempotencyKey = "create-completed";
        var expected = CreateSummary(
            Guid.NewGuid(),
            command.Fecha,
            command.Hora,
            patientId,
            "Reposición previa",
            actorUserId,
            actorUserId,
            DateTimeOffset.Parse("2026-05-03T13:00:00+00:00"),
            true,
            false,
            false,
            null,
            null,
            CreateLookup(patientId, "Paciente Uno", "12345678", true),
            null,
            null,
            CreateLookup(actorUserId, "Operador", null, true));

        SetupCompleted(CreateOperation(command), idempotencyKey, expected);

        var result = await Sut.CreateAsync(actorUserId, command, idempotencyKey, CancellationToken.None);

        Assert.Equal(expected, result);
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenIdempotencyPending_ThrowsConflictException()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var command = CreateCommand(new DateOnly(2026, 5, 3), new TimeOnly(14, 0), patientId, operadorCamaraId: null);
        var idempotencyKey = "create-pending";

        SetupPending(CreateOperation(command), idempotencyKey);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => Sut.CreateAsync(actorUserId, command, idempotencyKey, CancellationToken.None));

        Assert.Equal("operation_pending", exception.Code);
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenIdempotencyMismatch_ThrowsConflictException()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var command = CreateCommand(new DateOnly(2026, 5, 3), new TimeOnly(15, 0), patientId, operadorCamaraId: null);
        var idempotencyKey = "create-mismatch";

        SetupMismatch(CreateOperation(command), idempotencyKey);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => Sut.CreateAsync(actorUserId, command, idempotencyKey, CancellationToken.None));

        Assert.Equal("idempotency_mismatch", exception.Code);
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateTurnExists_ThrowsConflictExceptionAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var command = CreateCommand(new DateOnly(2026, 5, 3), new TimeOnly(16, 0), patientId, operadorCamaraId: null);
        var idempotencyKey = "create-duplicate";
        var actor = CreateActor(actorUserId, permission: "turnos.fuera_horario");
        var patient = CreatePatient(patientId, "Paciente Uno");
        var existing = CreateTurn(Guid.NewGuid(), command.Fecha, command.Hora, patientId, actorUserId, actorUserId, "Duplicado", false, false, false, null, null);

        SetupAcquired(CreateOperation(command), idempotencyKey);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(actor);
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        outOfHoursTurnRepository.GetByDateAsync(command.Fecha, Arg.Any<CancellationToken>()).Returns([existing]);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => Sut.CreateAsync(actorUserId, command, idempotencyKey, CancellationToken.None));

        Assert.Equal("Ya existe un turno fuera de horario para ese paciente y horario.", exception.Message);
        await idempotencyStore.Received(1).FailAsync(CreateOperation(command), idempotencyKey, Arg.Any<CancellationToken>());
        await outOfHoursTurnRepository.DidNotReceive().AddAsync(Arg.Any<OutOfHoursTurn>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenAllowedAndUnique_CreatesAndCompletesIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var medicoUserId = Guid.NewGuid();
        var command = CreateCommand(new DateOnly(2026, 5, 3), new TimeOnly(17, 0), patientId, operadorCamaraId: null, monoxidoMedicoUserId: medicoUserId);
        var idempotencyKey = "create-ok";
        var actor = CreateActor(actorUserId, permission: "turnos.fuera_horario", nombre: "Operador Cámara");
        var patient = CreatePatient(patientId, "Paciente Uno");
        var medicoUser = CreateActor(medicoUserId, permission: null, nombre: "Dr. Usuario");
        OutOfHoursTurn? addedTurn = null;

        SetupAcquired(CreateOperation(command), idempotencyKey);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(actor);
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        userRepository.GetByIdAsync(medicoUserId, Arg.Any<CancellationToken>()).Returns(medicoUser);
        outOfHoursTurnRepository.GetByDateAsync(command.Fecha, Arg.Any<CancellationToken>()).Returns([]);
        outOfHoursTurnRepository.AddAsync(Arg.Do<OutOfHoursTurn>(turn => addedTurn = turn), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(actor);

        var result = await Sut.CreateAsync(actorUserId, command, idempotencyKey, CancellationToken.None);

        Assert.NotNull(addedTurn);
        Assert.Equal(command.Fecha, result.Fecha);
        Assert.Equal(command.Hora, result.Hora);
        Assert.Equal(patientId, result.PacienteId);
        Assert.Equal(actorUserId, result.CreadoPor);
        Assert.Equal(actorUserId, result.OperadorCamaraId);
        Assert.Equal(medicoUserId, result.MonoxidoMedicoUser!.Id);
        Assert.Equal("Dr. Usuario", result.MonoxidoMedicoUser.Nombre);
        await idempotencyStore.Received(1).CompleteAsync(CreateOperation(command), idempotencyKey, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenMedicoComesFromGenericFields_PersistsEvenWithoutMonoxidoFields()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var medicoUserId = Guid.NewGuid();
        var command = new OutOfHoursTurnCreateCommand(
            new DateOnly(2026, 5, 3),
            new TimeOnly(18, 0),
            patientId,
            null,
            "Notas",
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false,
            MonoxidoMedicoId: null,
            MonoxidoMedicoUserId: null,
            MedicoId: null,
            MedicoUserId: medicoUserId);
        var idempotencyKey = "create-generic-medico-ok";
        var actor = CreateActor(actorUserId, permission: "turnos.fuera_horario", nombre: "Operador Cámara");
        var patient = CreatePatient(patientId, "Paciente Uno");
        var medicoUser = CreateActor(medicoUserId, permission: null, nombre: "Dr. Usuario");

        SetupAcquired(CreateOperation(command), idempotencyKey);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(actor);
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        userRepository.GetByIdAsync(medicoUserId, Arg.Any<CancellationToken>()).Returns(medicoUser);
        outOfHoursTurnRepository.GetByDateAsync(command.Fecha, Arg.Any<CancellationToken>()).Returns([]);

        var result = await Sut.CreateAsync(actorUserId, command, idempotencyKey, CancellationToken.None);

        Assert.Equal(medicoUserId, result.MonoxidoMedicoUser!.Id);
        Assert.Equal("Dr. Usuario", result.MonoxidoMedicoUser.Nombre);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenActorMissing_ThrowsUnauthorizedExceptionAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var idempotencyKey = "cancel-unauthorized";

        SetupAcquired(CancelOperation(turnoId), idempotencyKey);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => Sut.CancelAsync(actorUserId, turnoId, idempotencyKey, CancellationToken.None));

        await idempotencyStore.Received(1).FailAsync(CancelOperation(turnoId), idempotencyKey, Arg.Any<CancellationToken>());
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenActorForbidden_ThrowsForbiddenExceptionAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var idempotencyKey = "cancel-forbidden";

        SetupAcquired(CancelOperation(turnoId), idempotencyKey);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(CreateActor(actorUserId, permission: null));

        await Assert.ThrowsAsync<ForbiddenException>(() => Sut.CancelAsync(actorUserId, turnoId, idempotencyKey, CancellationToken.None));

        await idempotencyStore.Received(1).FailAsync(CancelOperation(turnoId), idempotencyKey, Arg.Any<CancellationToken>());
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenIdempotencyCompleted_ReturnsStoredResponse()
    {
        var actorUserId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var idempotencyKey = "cancel-completed";
        var expected = CreateSummary(
            turnoId,
            new DateOnly(2026, 5, 4),
            new TimeOnly(8, 30),
            Guid.NewGuid(),
            "Cancelada previamente",
            actorUserId,
            actorUserId,
            DateTimeOffset.Parse("2026-05-04T08:30:00+00:00"),
            false,
            false,
            false,
            null,
            null,
            CreateLookup(Guid.NewGuid(), "Paciente Uno", "12345678", true),
            null,
            null,
            CreateLookup(actorUserId, "Operador", null, true));

        SetupCompleted(CancelOperation(turnoId), idempotencyKey, expected);

        var result = await Sut.CancelAsync(actorUserId, turnoId, idempotencyKey, CancellationToken.None);

        Assert.Equal(expected, result);
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenIdempotencyPending_ThrowsConflictException()
    {
        var actorUserId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var idempotencyKey = "cancel-pending";

        SetupPending(CancelOperation(turnoId), idempotencyKey);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => Sut.CancelAsync(actorUserId, turnoId, idempotencyKey, CancellationToken.None));

        Assert.Equal("operation_pending", exception.Code);
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenIdempotencyMismatch_ThrowsConflictException()
    {
        var actorUserId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var idempotencyKey = "cancel-mismatch";

        SetupMismatch(CancelOperation(turnoId), idempotencyKey);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => Sut.CancelAsync(actorUserId, turnoId, idempotencyKey, CancellationToken.None));

        Assert.Equal("idempotency_mismatch", exception.Code);
        await outOfHoursTurnRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WhenTurnNotFound_ThrowsNotFoundExceptionAndFailsIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var idempotencyKey = "cancel-not-found";

        SetupAcquired(CancelOperation(turnoId), idempotencyKey);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(CreateActor(actorUserId, permission: "turnos.fuera_horario"));
        outOfHoursTurnRepository.GetByIdAsync(turnoId, Arg.Any<CancellationToken>()).Returns((OutOfHoursTurn?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => Sut.CancelAsync(actorUserId, turnoId, idempotencyKey, CancellationToken.None));

        await idempotencyStore.Received(1).FailAsync(CancelOperation(turnoId), idempotencyKey, Arg.Any<CancellationToken>());
        await outOfHoursTurnRepository.DidNotReceive().DeleteAsync(Arg.Any<OutOfHoursTurn>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WithExistingTurn_CancelsAndCompletesIdempotency()
    {
        var actorUserId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var turnoId = Guid.NewGuid();
        var idempotencyKey = "cancel-ok";
        var actor = CreateActor(actorUserId, permission: "turnos.fuera_horario", nombre: "Operador Cámara");
        var patient = CreatePatient(patientId, "Paciente Uno");
        var turno = CreateTurn(turnoId, new DateOnly(2026, 5, 4), new TimeOnly(9, 0), patientId, actorUserId, actorUserId, "A cancelar", false, false, false, null, null);

        SetupAcquired(CancelOperation(turnoId), idempotencyKey);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(actor);
        outOfHoursTurnRepository.GetByIdAsync(turnoId, Arg.Any<CancellationToken>()).Returns(turno);
        patientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        userRepository.GetByIdAsync(actorUserId, Arg.Any<CancellationToken>()).Returns(actor);

        var result = await Sut.CancelAsync(actorUserId, turnoId, idempotencyKey, CancellationToken.None);

        Assert.Equal(turno.Id, result.Id);
        Assert.Equal(turno.Fecha, result.Fecha);
        Assert.Equal(turno.Hora, result.Hora);
        Assert.Equal(turno.Notas, result.Notas);
        Assert.Equal(turno.CreadoPor, result.CreadoPor);
        Assert.Equal(actorUserId, result.OperadorCamaraId);
        Assert.Equal(patientId, result.Paciente!.Id);
        Assert.Equal(actorUserId, result.OperadorCamara!.Id);
        await outOfHoursTurnRepository.Received(1).DeleteAsync(turno, Arg.Any<CancellationToken>());
        await idempotencyStore.Received(1).CompleteAsync(CancelOperation(turnoId), idempotencyKey, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static OutOfHoursTurn CreateTurn(
        Guid id,
        DateOnly fecha,
        TimeOnly hora,
        Guid pacienteId,
        Guid creadoPor,
        Guid operadorCamaraId,
        string? notas,
        bool esMonoxido,
        bool monoxidoOrdenMedica,
        bool monoxidoResumenClinico,
        int? monoxidoMedicoId,
        Guid? monoxidoMedicoUserId)
    {
        return new OutOfHoursTurn(new OutOfHoursTurnCreateParams(
            id,
            fecha,
            hora,
            pacienteId,
            creadoPor,
            operadorCamaraId,
            notas,
            esMonoxido,
            monoxidoOrdenMedica,
            monoxidoResumenClinico,
            monoxidoMedicoId,
            monoxidoMedicoUserId));
    }

    private static Patient CreatePatient(Guid id, string nombre)
    {
        return new Patient(
            id,
            nombre,
            new PatientAdministrativeInfo("1144445555", "12345678", "12345678", 1),
            new PatientPortalInfo(true, "paciente-login"));
    }

    private static Medico CreateMedico(int id, string nombre)
    {
        return new Medico(nombre, id);
    }

    private static User CreateActor(Guid id, string? permission, string? nombre = null)
    {
        var user = new User(new UserCreateParams(
            id,
            $"user-{id:N}",
            $"user-{id:N}@medicalcenter.local",
            "hash",
            true,
            true));

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            user.UpdateProfileName(nombre);
        }

        if (!string.IsNullOrWhiteSpace(permission))
        {
            user.SetRoles([new Role(new RoleCreateParams(Guid.NewGuid(), "test-role", "Test Role", [permission]))]);
        }

        return user;
    }

    private static OutOfHoursTurnCreateCommand CreateCommand(DateOnly fecha, TimeOnly hora, Guid pacienteId, Guid? operadorCamaraId, Guid? monoxidoMedicoUserId = null)
    {
        return new OutOfHoursTurnCreateCommand(
            fecha,
            hora,
            pacienteId,
            operadorCamaraId,
            "Notas",
            true,
            false,
            true,
            7,
            monoxidoMedicoUserId);
    }

    private static string CreateOperation(OutOfHoursTurnCreateCommand command) =>
        $"turnos.fuera_horario.creaciones:{command.Fecha:yyyy-MM-dd}:{command.Hora:HH:mm}:{command.PacienteId}";

    private static string CancelOperation(Guid turnoId) => $"turnos.fuera_horario.cancelaciones:{turnoId}";

    private void SetupAcquired(string operation, string key)
    {
        idempotencyStore.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired));
    }

    private void SetupPending(string operation, string key)
    {
        idempotencyStore.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Pending));
    }

    private void SetupMismatch(string operation, string key)
    {
        idempotencyStore.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Mismatch));
    }

    private void SetupCompleted(string operation, string key, OutOfHoursTurnSummary summary)
    {
        idempotencyStore.ReserveAsync(operation, key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Completed, JsonSerializer.Serialize(summary, SerializerOptions)));
    }

    private static OutOfHoursTurnSummary CreateSummary(
        Guid id,
        DateOnly fecha,
        TimeOnly hora,
        Guid pacienteId,
        string? notas,
        Guid creadoPor,
        Guid operadorCamaraId,
        DateTimeOffset? createdAt,
        bool esMonoxido,
        bool monoxidoOrdenMedica,
        bool monoxidoResumenClinico,
        int? monoxidoMedicoId,
        Guid? monoxidoMedicoUserId,
        GuidLookupSummary? paciente,
        IntLookupSummary? monoxidoMedico,
        GuidLookupSummary? monoxidoMedicoUser,
        GuidLookupSummary? operadorCamara)
    {
        return new OutOfHoursTurnSummary(
            id,
            fecha,
            hora,
            pacienteId,
            notas,
            creadoPor,
            operadorCamaraId,
            createdAt,
            esMonoxido,
            monoxidoOrdenMedica,
            monoxidoResumenClinico,
            monoxidoMedicoId,
            monoxidoMedicoUserId,
            paciente,
            monoxidoMedico,
            monoxidoMedicoUser,
            operadorCamara);
    }

    private static GuidLookupSummary CreateLookup(Guid id, string nombre, string? documentoIdentidad, bool activo) =>
        new(id, nombre, documentoIdentidad, null, activo);
}
