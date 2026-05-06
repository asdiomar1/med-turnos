using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Helpers;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Appointments;

/// <summary>
/// Unit tests for AppointmentsService extended command methods: HoldAsync, ConfirmHoldAsync, ReleaseHoldAsync.
/// Work Unit 4 of T2 - Commands Extended Tests.
/// </summary>
public sealed class AppointmentsServiceExtendedCommandsTests : IClassFixture<AppointmentsServiceTestFixture>
{
    private readonly AppointmentsServiceTestFixture _fixture;

    public AppointmentsServiceExtendedCommandsTests(AppointmentsServiceTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    #region HoldAsync Tests

    [Fact]
    public async Task HoldAsync_WithAvailableSlot_HoldsAppointment()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-123";
        var operation = $"turnos.apartados:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsLibre()
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.TryCommitAsync(Arg.Any<CancellationToken>()).Returns(true);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act
        var result = await sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("apartado", result.Estado);
        Assert.Equal(patientId, result.PacienteId);
        Assert.Equal(actorUserId, result.ApartadoPorUserId);
        await _fixture.IdempotencyStore.ShouldCompleteIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task HoldAsync_WithNullIdempotencyKey_ThrowsValidationException()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.HoldAsync(Guid.NewGuid(), Guid.NewGuid(), "", command, CancellationToken.None));

        Assert.Contains("Idempotency-Key", ex.Message);
    }

    [Fact]
    public async Task HoldAsync_WhenPatientNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-patient-notfound";
        var operation = $"turnos.apartados:{slotId}";

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("Paciente no encontrado", ex.Message);
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task HoldAsync_WhenPatientInactive_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-patient-inactive";
        var operation = $"turnos.apartados:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Inactive().Build();
        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("no se encuentra activo", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task HoldAsync_WhenAppointmentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-appointment-notfound";
        var operation = $"turnos.apartados:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns((Appointment?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("Turno no encontrado", ex.Message);
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task HoldAsync_WhenAppointmentIsPast_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-past";
        var operation = $"turnos.apartados:{slotId}";

        // Set clock to future date so the appointment (May 2) is in the past
        _fixture.Clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(2026, 5, 2)
            .AsLibre()
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("turnos pasados", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task HoldAsync_WhenSlotNotReservable_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-not-reservable";
        var operation = $"turnos.apartados:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsOcupado(Guid.NewGuid())
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task HoldAsync_WhenSlotAlreadyHeld_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-already-held";
        var operation = $"turnos.apartados:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsApartado()
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task HoldAsync_WhenTryCommitFails_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-concurrency";
        var operation = $"turnos.apartados:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsLibre()
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.TryCommitAsync(Arg.Any<CancellationToken>()).Returns(false);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("no esta disponible", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task HoldAsync_WithCompletedIdempotency_ReturnsCachedResult()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-completed";
        var operation = $"turnos.apartados:{slotId}";

        var cachedResult = new AppointmentSummary(
            Id: slotId,
            Fecha: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Hora: new TimeOnly(10, 0),
            Lugar: 1,
            Estado: "apartado",
            PacienteId: patientId,
            CamaraId: null,
            BlockId: null,
            TandaId: null,
            ApartadoPorUserId: actorUserId,
            ApartadoTs: DateTimeOffset.UtcNow,
            EsBloqueCompleto: false,
            EsTanda: false,
            ReferidoTercero: false,
            ReferenteId: null,
            ModalidadCobro: "particular",
            ObraSocialId: null,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: null,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: null,
            MedicoUserId: null,
            EsNuevoIngreso: false,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false,
            CreatedAt: DateTimeOffset.UtcNow);

        _fixture.IdempotencyStore.SetupCompleted(operation, idempotencyKey, cachedResult);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act
        var result = await sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("apartado", result.Estado);
        await _fixture.PatientRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HoldAsync_WithPendingIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-pending";
        var operation = $"turnos.apartados:{slotId}";

        _fixture.IdempotencyStore.SetupPending(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Equal("operation_pending", ex.Code);
    }

    [Fact]
    public async Task HoldAsync_WithMismatchIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-hold-mismatch";
        var operation = $"turnos.apartados:{slotId}";

        _fixture.IdempotencyStore.SetupMismatch(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.HoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Equal("idempotency_mismatch", ex.Code);
    }

    #endregion

    #region ConfirmHoldAsync Tests

    [Fact]
    public async Task ConfirmHoldAsync_WithHeldSlot_ConfirmsAppointment()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-123";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsApartado(patientId, actorUserId)
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.GetOccupiedByPacienteOnDateAsync(
            patientId, futureDate, Arg.Any<CancellationToken>()).Returns(Array.Empty<Appointment>());
        _fixture.AppointmentRepository.TryCommitWithPatientLockAsync(
            patientId, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act
        var result = await sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ocupado", result.Estado);
        Assert.Equal(patientId, result.PacienteId);
        await _fixture.IdempotencyStore.ShouldCompleteIdempotencyAsync(operation, idempotencyKey);
        await _fixture.WhatsappService.Received(1).EnqueueTurnoConfirmadoAsync(
            Arg.Any<Appointment>(), "turnos_confirmacion_apartado", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmHoldAsync_WithNullIdempotencyKey_ThrowsValidationException()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ConfirmHoldAsync(Guid.NewGuid(), Guid.NewGuid(), "", command, CancellationToken.None));

        Assert.Contains("Idempotency-Key", ex.Message);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenPatientNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-patient-notfound";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("Paciente no encontrado", ex.Message);
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenPatientInactive_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-patient-inactive";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Inactive().Build();
        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("no se encuentra activo", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenAppointmentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-appointment-notfound";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns((Appointment?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("Turno no encontrado", ex.Message);
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenAppointmentIsPast_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-past";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";

        // Set clock to future date so the appointment (May 2) is in the past
        _fixture.Clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(2026, 5, 2)
            .AsApartado(patientId, actorUserId)
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("turnos pasados", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenAppointmentNotHeld_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-not-held";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsLibre() // Not held
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenHeldByDifferentPatient_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var differentPatientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-different-patient";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        // Appointment held by different patient
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsApartado(differentPatientId, actorUserId)
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenNoPatientProvided_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-no-patient";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        // Appointment held but no patient associated (null PatientId in hold)
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsApartado(null, actorUserId)
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(null); // No patient provided

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("paciente requerido", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WithConsecutiveAppointment_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-consecutive";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .WithHora(10, 0)
            .AsApartado(patientId, actorUserId)
            .Build();

        // Patient already has appointment at 11:00 (60 min after the held slot)
        var existingAppointment = new AppointmentBuilder()
            .WithFecha(futureDate)
            .WithHora(11, 0)
            .AsOcupado(patientId)
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.GetOccupiedByPacienteOnDateAsync(
            patientId, futureDate, Arg.Any<CancellationToken>()).Returns(new[] { existingAppointment });
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("consecutivos", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenTryCommitFails_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-concurrency";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsApartado(patientId, actorUserId)
            .Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.GetOccupiedByPacienteOnDateAsync(
            patientId, futureDate, Arg.Any<CancellationToken>()).Returns(Array.Empty<Appointment>());
        _fixture.AppointmentRepository.TryCommitWithPatientLockAsync(
            patientId, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("concurrencia", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WithCompletedIdempotency_ReturnsCachedResult()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-completed";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";

        var cachedResult = new AppointmentSummary(
            Id: slotId,
            Fecha: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Hora: new TimeOnly(10, 0),
            Lugar: 1,
            Estado: "ocupado",
            PacienteId: patientId,
            CamaraId: null,
            BlockId: null,
            TandaId: null,
            ApartadoPorUserId: null,
            ApartadoTs: null,
            EsBloqueCompleto: false,
            EsTanda: false,
            ReferidoTercero: false,
            ReferenteId: null,
            ModalidadCobro: "particular",
            ObraSocialId: null,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: null,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: null,
            MedicoUserId: null,
            EsNuevoIngreso: false,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false,
            CreatedAt: DateTimeOffset.UtcNow);

        _fixture.IdempotencyStore.SetupCompleted(operation, idempotencyKey, cachedResult);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act
        var result = await sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ocupado", result.Estado);
        await _fixture.PatientRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmHoldAsync_WithPendingIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-pending";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";

        _fixture.IdempotencyStore.SetupPending(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Equal("operation_pending", ex.Code);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WithMismatchIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-confirm-mismatch";
        var operation = $"turnos.apartados.confirmaciones:{slotId}";

        _fixture.IdempotencyStore.SetupMismatch(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateHoldCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ConfirmHoldAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Equal("idempotency_mismatch", ex.Code);
    }

    #endregion

    #region ReleaseHoldAsync Tests

    [Fact]
    public async Task ReleaseHoldAsync_WithHeldSlot_ReleasesAppointment()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-release-123";
        var operation = $"turnos.apartados.liberaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsApartado(patientId, actorUserId)
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.TryCommitAsync(Arg.Any<CancellationToken>()).Returns(true);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.ReleaseHoldAsync(actorUserId, slotId, idempotencyKey, "Liberado por admin", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("libre", result.Estado);
        Assert.Null(result.PacienteId);
        await _fixture.IdempotencyStore.ShouldCompleteIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WithNullIdempotencyKey_ThrowsValidationException()
    {
        // Arrange
        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ReleaseHoldAsync(Guid.NewGuid(), Guid.NewGuid(), "", null, CancellationToken.None));

        Assert.Contains("Idempotency-Key", ex.Message);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenAppointmentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-release-notfound";
        var operation = $"turnos.apartados.liberaciones:{slotId}";

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns((Appointment?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.ReleaseHoldAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Contains("Turno no encontrado", ex.Message);
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenAppointmentIsPast_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-release-past";
        var operation = $"turnos.apartados.liberaciones:{slotId}";

        // Set clock to future date so the appointment (May 2) is in the past
        _fixture.Clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(2026, 5, 2)
            .AsApartado(patientId, actorUserId)
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ReleaseHoldAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Contains("turnos pasados", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenAppointmentNotHeld_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-release-not-held";
        var operation = $"turnos.apartados.liberaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsOcupado(patientId) // Occupied, not held
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ReleaseHoldAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenSlotAlreadyReleased_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-release-already-released";
        var operation = $"turnos.apartados.liberaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        // Already released (libre status means it was released)
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ReleaseHoldAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenTryCommitFails_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-release-concurrency";
        var operation = $"turnos.apartados.liberaciones:{slotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(futureDate)
            .AsApartado(patientId, actorUserId)
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.TryCommitAsync(Arg.Any<CancellationToken>()).Returns(false);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ReleaseHoldAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Contains("concurrencia", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WithCompletedIdempotency_ReturnsCachedResult()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-release-completed";
        var operation = $"turnos.apartados.liberaciones:{slotId}";

        var cachedResult = new AppointmentSummary(
            Id: slotId,
            Fecha: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Hora: new TimeOnly(10, 0),
            Lugar: 1,
            Estado: "libre",
            PacienteId: null,
            CamaraId: null,
            BlockId: null,
            TandaId: null,
            ApartadoPorUserId: null,
            ApartadoTs: null,
            EsBloqueCompleto: false,
            EsTanda: false,
            ReferidoTercero: false,
            ReferenteId: null,
            ModalidadCobro: "particular",
            ObraSocialId: null,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: null,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: null,
            MedicoUserId: null,
            EsNuevoIngreso: false,
            EsMonoxido: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false,
            CreatedAt: DateTimeOffset.UtcNow);

        _fixture.IdempotencyStore.SetupCompleted(operation, idempotencyKey, cachedResult);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.ReleaseHoldAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("libre", result.Estado);
        await _fixture.AppointmentRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseHoldAsync_WithPendingIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-release-pending";
        var operation = $"turnos.apartados.liberaciones:{slotId}";

        _fixture.IdempotencyStore.SetupPending(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ReleaseHoldAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Equal("operation_pending", ex.Code);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WithMismatchIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-release-mismatch";
        var operation = $"turnos.apartados.liberaciones:{slotId}";

        _fixture.IdempotencyStore.SetupMismatch(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.ReleaseHoldAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Equal("idempotency_mismatch", ex.Code);
    }

    #endregion

    #region Helper Methods

    private static HoldAppointmentCommand CreateHoldCommand(Guid? patientId)
    {
        return new HoldAppointmentCommand(
            PacienteId: patientId,
            EsMonoxido: false,
            ReferidoTercero: false,
            ReferenteId: null,
            ModalidadCobro: "particular",
            ObraSocialId: null,
            NumeroAutorizacion: null,
            SesionesAutorizadas: null,
            CicloObraSocialId: null,
            IniciarNuevoCicloObraSocial: false,
            ConvenioCorroborado: false,
            MedicoId: null,
            MedicoUserId: null,
            EsNuevoIngreso: false,
            MonoxidoOrdenMedica: false,
            MonoxidoResumenClinico: false);
    }

    #endregion
}
