using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Application.Features.WhatsApp;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Helpers;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Appointments;

/// <summary>
/// Unit tests for AppointmentsService command methods: AssignAsync, CancelAsync, RescheduleAsync.
/// Work Unit 3 of T2 - Commands Core Tests.
/// </summary>
public sealed class AppointmentsServiceCommandsTests : IClassFixture<AppointmentsServiceTestFixture>
{
    private readonly AppointmentsServiceTestFixture _fixture;

    public AppointmentsServiceCommandsTests(AppointmentsServiceTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    #region AssignAsync Tests

    [Fact]
    public async Task AssignAsync_WithValidData_CreatesAppointment()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-assign-123";
        var operation = $"turnos.asignaciones:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder().WithId(slotId).AsLibre().Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.TryCommitWithPatientLockAsync(
            patientId, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), null, Arg.Any<CancellationToken>()).Returns(true);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act
        var result = await sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ocupado", result.Estado);
        Assert.Equal(patientId, result.PacienteId);
        await _fixture.IdempotencyStore.ShouldCompleteIdempotencyAsync(operation, idempotencyKey);
        await _fixture.WhatsappService.Received(1).EnqueueTurnoConfirmadoAsync(
            Arg.Any<Appointment>(), "turnos_asignacion", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignAsync_WithNullIdempotencyKey_ThrowsValidationException()
    {
        // Arrange
        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.AssignAsync(Guid.NewGuid(), Guid.NewGuid(), "", command, CancellationToken.None));

        Assert.Contains("Idempotency-Key", ex.Message);
    }

    [Fact]
    public async Task AssignAsync_WhenPatientNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-patient-notfound";
        var operation = $"turnos.asignaciones:{slotId}";

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("Paciente no encontrado", ex.Message);
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task AssignAsync_WhenPatientInactive_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-patient-inactive";
        var operation = $"turnos.asignaciones:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Inactive().Build();
        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("no se encuentra activo", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task AssignAsync_WhenAppointmentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-appointment-notfound";
        var operation = $"turnos.asignaciones:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns((Appointment?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("Turno no encontrado", ex.Message);
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task AssignAsync_WhenAppointmentIsPast_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-past-appointment";
        var operation = $"turnos.asignaciones:{slotId}";

        // Set clock to future date so the appointment (May 2) is in the past
        _fixture.Clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder().WithId(slotId).WithFecha(2026, 5, 2).AsLibre().Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("turnos pasados", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task AssignAsync_WhenSlotNotReservable_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-not-reservable";
        var operation = $"turnos.asignaciones:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder().WithId(slotId).AsOcupado(Guid.NewGuid()).Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("no está disponible", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task AssignAsync_WhenSlotIsApartado_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-apartado";
        var operation = $"turnos.asignaciones:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder().WithId(slotId).AsApartado().Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("no está disponible", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task AssignAsync_WithConsecutiveAppointmentWithin60Minutes_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-consecutive";
        var operation = $"turnos.asignaciones:{slotId}";

        // Current appointment at 10:00
        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(_fixture.GetTodayInArgentina().AddDays(1))
            .WithHora(10, 0)
            .AsLibre()
            .Build();

        // Patient already has appointment at 11:00 (60 min difference)
        var existingAppointment = new AppointmentBuilder()
            .WithFecha(_fixture.GetTodayInArgentina().AddDays(1))
            .WithHora(11, 0)
            .AsOcupado(patientId)
            .Build();

        var patient = new PatientBuilder().WithId(patientId).Active().Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.GetOccupiedByPacienteOnDateAsync(
            patientId, appointment.Fecha, Arg.Any<CancellationToken>()).Returns(new[] { existingAppointment });
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("consecutivos", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task AssignAsync_WhenTryCommitFails_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-concurrency";
        var operation = $"turnos.asignaciones:{slotId}";

        var patient = new PatientBuilder().WithId(patientId).Active().Build();
        var appointment = new AppointmentBuilder().WithId(slotId).AsLibre().Build();

        _fixture.PatientRepository.GetByIdAsync(patientId, Arg.Any<CancellationToken>()).Returns(patient);
        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.TryCommitWithPatientLockAsync(
            patientId, Arg.Any<DateOnly>(), Arg.Any<TimeOnly>(), null, Arg.Any<CancellationToken>()).Returns(false);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("no está disponible", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task AssignAsync_WithCompletedIdempotency_ReturnsCachedResult()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-completed";
        var operation = $"turnos.asignaciones:{slotId}";

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
        var command = CreateAssignCommand(patientId);

        // Act
        var result = await sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patientId, result.PacienteId);
        Assert.Equal("ocupado", result.Estado);
        await _fixture.PatientRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignAsync_WithPendingIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-pending";
        var operation = $"turnos.asignaciones:{slotId}";

        _fixture.IdempotencyStore.SetupPending(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("en proceso", ex.Message.ToLower());
        Assert.Equal("operation_pending", ex.Code);
    }

    [Fact]
    public async Task AssignAsync_WithMismatchIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-mismatch";
        var operation = $"turnos.asignaciones:{slotId}";

        _fixture.IdempotencyStore.SetupMismatch(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = CreateAssignCommand(patientId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AssignAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("Idempotency-Key", ex.Message);
        Assert.Equal("idempotency_mismatch", ex.Code);
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_WithValidData_CancelsAppointment()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-cancel-123";
        var operation = $"turnos.cancelaciones:{slotId}";

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(_fixture.GetTodayInArgentina().AddDays(1))
            .AsOcupado(patientId)
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.TryCommitAsync(Arg.Any<CancellationToken>()).Returns(true);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.CancelAsync(actorUserId, slotId, idempotencyKey, "Paciente solicitó", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cancelado", result.Estado);
        await _fixture.IdempotencyStore.ShouldCompleteIdempotencyAsync(operation, idempotencyKey);
        await _fixture.WhatsappService.Received(1).EnqueueTurnoCancelacionAsync(
            Arg.Any<Appointment>(), "turnos_cancelacion", idempotencyKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WithNullIdempotencyKey_ThrowsValidationException()
    {
        // Arrange
        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.CancelAsync(Guid.NewGuid(), Guid.NewGuid(), "", null, CancellationToken.None));

        Assert.Contains("Idempotency-Key", ex.Message);
    }

    [Fact]
    public async Task CancelAsync_WhenAppointmentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-cancel-notfound";
        var operation = $"turnos.cancelaciones:{slotId}";

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns((Appointment?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.CancelAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Contains("Turno no encontrado", ex.Message);
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task CancelAsync_WhenAppointmentIsPast_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-cancel-past";
        var operation = $"turnos.cancelaciones:{slotId}";

        // Set clock to future date so the appointment (May 2) is in the past
        _fixture.Clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(2026, 5, 2)
            .AsOcupado(patientId)
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.CancelAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Contains("turnos pasados", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task CancelAsync_WhenAppointmentNotOccupied_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-cancel-not-occupied";
        var operation = $"turnos.cancelaciones:{slotId}";

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(_fixture.GetTodayInArgentina().AddDays(1))
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.CancelAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task CancelAsync_WhenTryCommitFails_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-cancel-concurrency";
        var operation = $"turnos.cancelaciones:{slotId}";

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(_fixture.GetTodayInArgentina().AddDays(1))
            .AsOcupado(patientId)
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(slotId, Arg.Any<CancellationToken>()).Returns(appointment);
        _fixture.AppointmentRepository.TryCommitAsync(Arg.Any<CancellationToken>()).Returns(false);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.CancelAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Contains("concurrencia", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task CancelAsync_WithCompletedIdempotency_ReturnsCachedResult()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-cancel-completed";
        var operation = $"turnos.cancelaciones:{slotId}";

        var cachedResult = new AppointmentSummary(
            Id: slotId,
            Fecha: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Hora: new TimeOnly(10, 0),
            Lugar: 1,
            Estado: "cancelado",
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

        // Act
        var result = await sut.CancelAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cancelado", result.Estado);
        await _fixture.AppointmentRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_WithPendingIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-cancel-pending";
        var operation = $"turnos.cancelaciones:{slotId}";

        _fixture.IdempotencyStore.SetupPending(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.CancelAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Equal("operation_pending", ex.Code);
    }

    [Fact]
    public async Task CancelAsync_WithMismatchIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-cancel-mismatch";
        var operation = $"turnos.cancelaciones:{slotId}";

        _fixture.IdempotencyStore.SetupMismatch(operation, idempotencyKey);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.CancelAsync(actorUserId, slotId, idempotencyKey, null, CancellationToken.None));

        Assert.Equal("idempotency_mismatch", ex.Code);
    }

    #endregion

    #region RescheduleAsync Tests

    [Fact]
    public async Task RescheduleAsync_WithSameSlot_ThrowsValidationException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-same";
        var operation = $"turnos.reprogramaciones:{slotId}";

        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(slotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.RescheduleAsync(actorUserId, slotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("destino debe ser distinto", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_SingleScope_WithValidData_ReschedulesAppointment()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-single";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(futureDate)
            .WithHora(9, 0)
            .AsOcupado(patientId)
            .Build();

        var targetAppointment = new AppointmentBuilder()
            .WithId(targetSlotId)
            .WithFecha(futureDate)
            .WithHora(11, 0)
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns(targetAppointment);
        _fixture.AppointmentRepository.TryCommitAsync(Arg.Any<CancellationToken>()).Returns(true);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act
        var result = await sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await _fixture.IdempotencyStore.ShouldCompleteIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_SingleScope_SetsSourceStatusToReprogramado()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-status";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(futureDate)
            .WithHora(9, 0)
            .AsOcupado(patientId)
            .Build();

        var targetAppointment = new AppointmentBuilder()
            .WithId(targetSlotId)
            .WithFecha(futureDate)
            .WithHora(11, 0)
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns(targetAppointment);
        _fixture.AppointmentRepository.TryCommitAsync(Arg.Any<CancellationToken>()).Returns(true);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act
        await sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.Equal(AppointmentStatus.Reprogramado, sourceAppointment.Status);
    }

    [Fact]
    public async Task RescheduleAsync_WhenSourceNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-source-notfound";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns((Appointment?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("origen no encontrado", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_WhenTargetNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-target-notfound";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(futureDate)
            .AsOcupado(patientId)
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns((Appointment?)null);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("destino no encontrado", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_WhenSourceIsPast_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-past-source";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";

        // Set clock to future date
        _fixture.Clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(2026, 5, 2) // Past date
            .AsOcupado(patientId)
            .Build();

        var targetAppointment = new AppointmentBuilder()
            .WithId(targetSlotId)
            .WithFecha(2026, 5, 15) // Future date
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns(targetAppointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("turnos pasados", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_WhenTargetIsPast_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-past-target";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";

        // Set clock to future date
        _fixture.Clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));

        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(2026, 5, 15) // Future date
            .AsOcupado(patientId)
            .Build();

        var targetAppointment = new AppointmentBuilder()
            .WithId(targetSlotId)
            .WithFecha(2026, 5, 2) // Past date
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns(targetAppointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("turnos pasados", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_SingleScope_WhenSourceNotOccupied_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-not-occupied";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(futureDate)
            .AsLibre() // Not occupied
            .Build();

        var targetAppointment = new AppointmentBuilder()
            .WithId(targetSlotId)
            .WithFecha(futureDate)
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns(targetAppointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("solo se pueden reprogramar turnos ocupados", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_SingleScope_WhenSourceIsBlock_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-block";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(futureDate)
            .AsOcupado(patientId)
            .WithBlockId(blockId) // Is a block
            .Build();

        var targetAppointment = new AppointmentBuilder()
            .WithId(targetSlotId)
            .WithFecha(futureDate)
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns(targetAppointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("bloques completos no se reprograman", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_SingleScope_WhenTargetNotReservable_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-target-occupied";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(futureDate)
            .AsOcupado(patientId)
            .Build();

        var targetAppointment = new AppointmentBuilder()
            .WithId(targetSlotId)
            .WithFecha(futureDate)
            .AsOcupado(Guid.NewGuid()) // Already occupied
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns(targetAppointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("destino ya no", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_WithConsecutiveBlocking_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-consecutive";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        // Source at 10:00
        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(futureDate)
            .WithHora(10, 0)
            .AsOcupado(patientId)
            .Build();

        // Target at 11:00 - patient already has appointment at 12:00 (60 min diff from target)
        var targetAppointment = new AppointmentBuilder()
            .WithId(targetSlotId)
            .WithFecha(futureDate)
            .WithHora(11, 0)
            .AsLibre()
            .Build();

        var existingAppointment = new AppointmentBuilder()
            .WithFecha(futureDate)
            .WithHora(12, 0) // 60 min after target
            .AsOcupado(patientId)
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns(targetAppointment);
        _fixture.AppointmentRepository.GetOccupiedByPacienteOnDateAsync(
            patientId, futureDate, Arg.Any<CancellationToken>()).Returns(new[] { existingAppointment });
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("consecutivos", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    [Fact]
    public async Task RescheduleAsync_WithCompletedIdempotency_ReturnsCachedResult()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-completed";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";

        var cachedResult = new AppointmentSummary(
            Id: targetSlotId,
            Fecha: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Hora: new TimeOnly(11, 0),
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
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act
        var result = await sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetSlotId, result.Id);
        await _fixture.AppointmentRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescheduleAsync_WithPendingIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-pending";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";

        _fixture.IdempotencyStore.SetupPending(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Equal("operation_pending", ex.Code);
    }

    [Fact]
    public async Task RescheduleAsync_WithMismatchIdempotency_ThrowsConflictException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-mismatch";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";

        _fixture.IdempotencyStore.SetupMismatch(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "normal");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Equal("idempotency_mismatch", ex.Code);
    }

    [Fact]
    public async Task RescheduleAsync_WithInvalidScope_ThrowsValidationException()
    {
        // Arrange
        var actorUserId = Guid.NewGuid();
        var sourceSlotId = Guid.NewGuid();
        var targetSlotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var idempotencyKey = "idem-reschedule-invalid-scope";
        var operation = $"turnos.reprogramaciones:{sourceSlotId}";
        var futureDate = _fixture.GetTodayInArgentina().AddDays(1);

        var sourceAppointment = new AppointmentBuilder()
            .WithId(sourceSlotId)
            .WithFecha(futureDate)
            .AsOcupado(patientId)
            .Build();

        var targetAppointment = new AppointmentBuilder()
            .WithId(targetSlotId)
            .WithFecha(futureDate)
            .AsLibre()
            .Build();

        _fixture.AppointmentRepository.GetByIdAsync(sourceSlotId, Arg.Any<CancellationToken>()).Returns(sourceAppointment);
        _fixture.AppointmentRepository.GetByIdAsync(targetSlotId, Arg.Any<CancellationToken>()).Returns(targetAppointment);
        _fixture.IdempotencyStore.SetupAcquired(operation, idempotencyKey);

        var sut = _fixture.CreateSut();
        var command = new RescheduleAppointmentCommand(targetSlotId, "invalid_scope");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.RescheduleAsync(actorUserId, sourceSlotId, idempotencyKey, command, CancellationToken.None));

        Assert.Contains("scope invalido", ex.Message.ToLower());
        await _fixture.IdempotencyStore.ShouldFailIdempotencyAsync(operation, idempotencyKey);
    }

    #endregion

    #region Helper Methods

    private static AssignAppointmentCommand CreateAssignCommand(Guid patientId)
    {
        return new AssignAppointmentCommand(
            PacienteId: patientId,
            EsTanda: false,
            TandaId: null,
            Accion: null,
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
            MonoxidoResumenClinico: false);
    }

    #endregion
}
