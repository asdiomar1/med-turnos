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
/// Unit tests for AppointmentsService block and portal operations.
/// Tests block assignments, cancellations, portal reservations, and utility methods.
/// </summary>
public sealed class AppointmentsServiceBlocksPortalTests : IClassFixture<AppointmentsServiceTestFixture>
{
    private readonly AppointmentsServiceTestFixture _fixture;

    public AppointmentsServiceBlocksPortalTests(AppointmentsServiceTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    #region AssignBlockAsync

    [Fact]
    public async Task AssignBlockAsync_WithAvailableSlots_AssignsBlockSuccessfully()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);
        var time = new TimeOnly(9, 0);
        var cameraId = 1;
        var idempotencyKey = $"block-{Guid.NewGuid()}";

        var patient = new PatientBuilder()
            .WithId(patientId)
            .Active()
            .Build();

        var slots = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(0)
                .WithCameraId(cameraId)
                .AsLibre()
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(1)
                .WithCameraId(cameraId)
                .AsLibre()
                .Build()
        };

        _fixture.PatientRepository
            .GetByIdAsync(patientId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _fixture.AppointmentRepository
            .GetByBlockAsync(date, time, cameraId, Arg.Any<CancellationToken>())
            .Returns(slots);
        _fixture.AppointmentRepository
            .TryCommitAsync(Arg.Any<CancellationToken>())
            .Returns(true);
        _fixture.IdempotencyStore
            .ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired, null));

        var command = new AssignBlockAppointmentsCommand(
            date, time, cameraId, patientId, false, null, false, null, "particular",
            null, null, null, null, false, false, null, null, false, false, false, false);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.AssignBlockAsync(actorId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        await _fixture.AppointmentRepository
            .Received(1)
            .TryCommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignBlockAsync_WithPartiallyOccupiedSlots_ThrowsConflictException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);
        var time = new TimeOnly(9, 0);
        var cameraId = 1;
        var idempotencyKey = $"block-{Guid.NewGuid()}";

        var patient = new PatientBuilder()
            .WithId(patientId)
            .Active()
            .Build();

        var slots = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(0)
                .WithCameraId(cameraId)
                .AsLibre()
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(1)
                .WithCameraId(cameraId)
                .AsOcupado(otherPatientId)
                .Build()
        };

        _fixture.PatientRepository
            .GetByIdAsync(patientId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _fixture.AppointmentRepository
            .GetByBlockAsync(date, time, cameraId, Arg.Any<CancellationToken>())
            .Returns(slots);
        _fixture.IdempotencyStore
            .ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired, null));

        var command = new AssignBlockAppointmentsCommand(
            date, time, cameraId, patientId, false, null, false, null, "particular",
            null, null, null, null, false, false, null, null, false, false, false, false);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => sut.AssignBlockAsync(actorId, idempotencyKey, command, CancellationToken.None));
        Assert.Contains("no esta completamente disponible", exception.Message);
    }

    [Fact]
    public async Task AssignBlockAsync_WithAllSlotsOccupied_ThrowsConflictException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);
        var time = new TimeOnly(9, 0);
        var cameraId = 1;
        var idempotencyKey = $"block-{Guid.NewGuid()}";

        var patient = new PatientBuilder()
            .WithId(patientId)
            .Active()
            .Build();

        var slots = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(0)
                .WithCameraId(cameraId)
                .AsOcupado(otherPatientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(1)
                .WithCameraId(cameraId)
                .AsOcupado(otherPatientId)
                .Build()
        };

        _fixture.PatientRepository
            .GetByIdAsync(patientId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _fixture.AppointmentRepository
            .GetByBlockAsync(date, time, cameraId, Arg.Any<CancellationToken>())
            .Returns(slots);
        _fixture.IdempotencyStore
            .ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired, null));

        var command = new AssignBlockAppointmentsCommand(
            date, time, cameraId, patientId, false, null, false, null, "particular",
            null, null, null, null, false, false, null, null, false, false, false, false);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => sut.AssignBlockAsync(actorId, idempotencyKey, command, CancellationToken.None));
        Assert.Contains("no esta completamente disponible", exception.Message);
    }

    #endregion

    #region CancelBlockAsync

    [Fact]
    public async Task CancelBlockAsync_WithExistingBlock_CancelsSuccessfully()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);
        var time = new TimeOnly(9, 0);
        var cameraId = 1;
        var idempotencyKey = $"cancel-block-{Guid.NewGuid()}";

        var slots = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(0)
                .WithCameraId(cameraId)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(1)
                .WithCameraId(cameraId)
                .AsOcupado(patientId)
                .Build()
        };

        // Set EsBloqueCompleto via reflection
        foreach (var slot in slots)
        {
            EntityReflectionHelper.SetProperty(slot, nameof(Appointment.EsBloqueCompleto), true);
        }

        _fixture.AppointmentRepository
            .GetByBlockAsync(date, time, cameraId, Arg.Any<CancellationToken>())
            .Returns(slots);
        _fixture.AppointmentRepository
            .TryCommitAsync(Arg.Any<CancellationToken>())
            .Returns(true);
        _fixture.IdempotencyStore
            .ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired, null));

        var command = new CancelBlockAppointmentsCommand(date, time, cameraId, patientId, "Test cancellation");

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.CancelBlockAsync(actorId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        await _fixture.AppointmentRepository
            .Received(1)
            .TryCommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelBlockAsync_WithMixedStatuses_CancelsOnlyOcupadoSlots()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);
        var time = new TimeOnly(9, 0);
        var cameraId = 1;
        var idempotencyKey = $"cancel-block-{Guid.NewGuid()}";

        var slots = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(0)
                .WithCameraId(cameraId)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(1)
                .WithCameraId(cameraId)
                .AsApartado(patientId, actorId)
                .Build()
        };

        // Set EsBloqueCompleto via reflection
        foreach (var slot in slots)
        {
            EntityReflectionHelper.SetProperty(slot, nameof(Appointment.EsBloqueCompleto), true);
        }

        _fixture.AppointmentRepository
            .GetByBlockAsync(date, time, cameraId, Arg.Any<CancellationToken>())
            .Returns(slots);
        _fixture.AppointmentRepository
            .TryCommitAsync(Arg.Any<CancellationToken>())
            .Returns(true);
        _fixture.IdempotencyStore
            .ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired, null));

        var command = new CancelBlockAppointmentsCommand(date, time, cameraId, patientId, "Test cancellation");

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.CancelBlockAsync(actorId, idempotencyKey, command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        await _fixture.AppointmentRepository
            .Received(1)
            .TryCommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelBlockAsync_WithNoMatchingSlots_ThrowsNotFoundException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);
        var time = new TimeOnly(9, 0);
        var cameraId = 1;
        var idempotencyKey = $"cancel-block-{Guid.NewGuid()}";

        var slots = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(0)
                .WithCameraId(cameraId)
                .AsOcupado(otherPatientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(1)
                .WithCameraId(cameraId)
                .AsOcupado(otherPatientId)
                .Build()
        };

        _fixture.AppointmentRepository
            .GetByBlockAsync(date, time, cameraId, Arg.Any<CancellationToken>())
            .Returns(slots);
        _fixture.IdempotencyStore
            .ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired, null));

        var command = new CancelBlockAppointmentsCommand(date, time, cameraId, patientId, "Test cancellation");

        var sut = _fixture.CreateSut();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => sut.CancelBlockAsync(actorId, idempotencyKey, command, CancellationToken.None));
        Assert.Contains("No se encontraron slots para el paciente indicado", exception.Message);
    }

    #endregion

    #region CancelTandaAsync

    [Fact]
    public async Task CancelTandaAsync_WithOccupiedSlots_CancelsSuccessfully()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var tandaId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);
        var time = new TimeOnly(9, 0);
        var idempotencyKey = $"cancel-tanda-{Guid.NewGuid()}";

        var slots = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(0)
                .WithTanda(tandaId)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date.AddDays(1))
                .WithHora(time)
                .WithLugar(0)
                .WithTanda(tandaId)
                .AsOcupado(patientId)
                .Build()
        };

        _fixture.AppointmentRepository
            .GetByTandaIdAsync(tandaId, Arg.Any<CancellationToken>())
            .Returns(slots);
        _fixture.AppointmentRepository
            .TryCommitAsync(Arg.Any<CancellationToken>())
            .Returns(true);
        _fixture.IdempotencyStore
            .ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired, null));

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.CancelTandaAsync(actorId, tandaId, idempotencyKey, "Test cancellation", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        await _fixture.AppointmentRepository
            .Received(1)
            .TryCommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelTandaAsync_WithNoOccupiedSlots_ThrowsNotFoundException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var tandaId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);
        var time = new TimeOnly(9, 0);
        var idempotencyKey = $"cancel-tanda-{Guid.NewGuid()}";

        var slots = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(time)
                .WithLugar(0)
                .WithTanda(tandaId)
                .AsLibre()
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date.AddDays(1))
                .WithHora(time)
                .WithLugar(0)
                .WithTanda(tandaId)
                .AsCancelado()
                .Build()
        };

        _fixture.AppointmentRepository
            .GetByTandaIdAsync(tandaId, Arg.Any<CancellationToken>())
            .Returns(slots);
        _fixture.IdempotencyStore
            .ReserveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyReservationResult(IdempotencyReservationState.Acquired, null));

        var sut = _fixture.CreateSut();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => sut.CancelTandaAsync(actorId, tandaId, idempotencyKey, "Test cancellation", CancellationToken.None));
        Assert.Contains("No se encontraron slots ocupados para la tanda solicitada", exception.Message);
    }

    #endregion

    #region UpdateOperativeAsync

    [Fact]
    public async Task UpdateOperativeAsync_WithValidSlot_UpdatesOperativeData()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);

        var actor = new UserBuilder()
            .WithId(actorId)
            .AsStaff()
            .WithPermission("turnos.asignar")
            .Build();

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(date)
            .WithHora(9, 0)
            .AsOcupado(patientId)
            .Build();

        _fixture.UserRepository
            .GetByIdAsync(actorId, Arg.Any<CancellationToken>())
            .Returns(actor);
        _fixture.AppointmentRepository
            .GetByIdAsync(slotId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        _fixture.AppointmentRepository
            .TryCommitAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new AppointmentOperativeCommand(
            false, null, "particular", null, null, null, null, false, false,
            null, false, false, false, false, null);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.UpdateOperativeAsync(actorId, slotId, command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await _fixture.AppointmentRepository
            .Received(1)
            .TryCommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateOperativeAsync_WithNonExistentSlot_ThrowsNotFoundException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        var actor = new UserBuilder()
            .WithId(actorId)
            .AsStaff()
            .WithPermission("turnos.asignar")
            .Build();

        _fixture.UserRepository
            .GetByIdAsync(actorId, Arg.Any<CancellationToken>())
            .Returns(actor);
        _fixture.AppointmentRepository
            .GetByIdAsync(slotId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        var command = new AppointmentOperativeCommand(
            false, null, "particular", null, null, null, null, false, false,
            null, false, false, false, false, null);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => sut.UpdateOperativeAsync(actorId, slotId, command, CancellationToken.None));
        Assert.Contains("Turno no encontrado", exception.Message);
    }

    #endregion

    #region UpdateOperativeByTandaAsync

    [Fact]
    public async Task UpdateOperativeByTandaAsync_WithValidTanda_UpdatesAllSlots()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var tandaId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);

        var actor = new UserBuilder()
            .WithId(actorId)
            .AsStaff()
            .WithPermission("turnos.tanda")
            .Build();

        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(9, 0)
                .WithTanda(tandaId)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date.AddDays(1))
                .WithHora(9, 0)
                .WithTanda(tandaId)
                .AsOcupado(patientId)
                .Build()
        };

        _fixture.UserRepository
            .GetByIdAsync(actorId, Arg.Any<CancellationToken>())
            .Returns(actor);
        _fixture.AppointmentRepository
            .GetByTandaIdAsync(tandaId, Arg.Any<CancellationToken>())
            .Returns(appointments);
        _fixture.AppointmentRepository
            .TryCommitAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new AppointmentOperativeCommand(
            false, null, "particular", null, null, null, null, false, false,
            null, false, false, false, false, null);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.UpdateOperativeByTandaAsync(actorId, tandaId, command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        await _fixture.AppointmentRepository
            .Received(1)
            .TryCommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateOperativeByTandaAsync_WithInvalidTanda_ReturnsEmptyList()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var tandaId = Guid.NewGuid();

        var actor = new UserBuilder()
            .WithId(actorId)
            .AsStaff()
            .WithPermission("turnos.tanda")
            .Build();

        _fixture.UserRepository
            .GetByIdAsync(actorId, Arg.Any<CancellationToken>())
            .Returns(actor);
        _fixture.AppointmentRepository
            .GetByTandaIdAsync(tandaId, Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.AppointmentRepository
            .TryCommitAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new AppointmentOperativeCommand(
            false, null, "particular", null, null, null, null, false, false,
            null, false, false, false, false, null);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.UpdateOperativeByTandaAsync(actorId, tandaId, command, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region RegisterBlockHistoryAsync

    [Fact]
    public async Task RegisterBlockHistoryAsync_WithValidEntries_RegistersHistory()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);

        var actor = new UserBuilder()
            .WithId(actorId)
            .AsStaff()
            .Build();

        _fixture.UserRepository
            .GetByIdAsync(actorId, Arg.Any<CancellationToken>())
            .Returns(actor);

        var entries = new List<BlockHistoryWriteCommand>
        {
            new(date, new TimeOnly(9, 0), 1, null, null, "bloqueo", null, "Test block"),
            new(date, new TimeOnly(10, 0), 1, null, null, "desbloqueo", null, "Test unblock")
        };

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.RegisterBlockHistoryAsync(actorId, entries, CancellationToken.None);

        // Assert
        Assert.Equal(2, result);
        await _fixture.BlockHistoryRepository
            .Received(1)
            .AddRangeAsync(Arg.Any<IEnumerable<BlockHistory>>(), Arg.Any<CancellationToken>());
        await _fixture.UnitOfWork
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterBlockHistoryAsync_WithEmptyAction_ThrowsValidationException()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);

        var actor = new UserBuilder()
            .WithId(actorId)
            .AsStaff()
            .Build();

        _fixture.UserRepository
            .GetByIdAsync(actorId, Arg.Any<CancellationToken>())
            .Returns(actor);

        var entries = new List<BlockHistoryWriteCommand>
        {
            new(date, new TimeOnly(9, 0), 1, null, null, "", null, "Test block")
        };

        var sut = _fixture.CreateSut();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => sut.RegisterBlockHistoryAsync(actorId, entries, CancellationToken.None));
        Assert.Contains("accion del historial es obligatoria", exception.Message);
    }

    #endregion

    #region ReservePortalAsync

    [Fact]
    public async Task ReservePortalAsync_AuthenticatedUser_ReservesAppointment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);

        var user = new UserBuilder()
            .WithId(userId)
            .WithPatientId(patientId)
            .AsPatient()
            .Build();

        var patient = new PatientBuilder()
            .WithId(patientId)
            .Active()
            .Build();

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(date)
            .WithHora(9, 0)
            .AsLibre()
            .Build();

        _fixture.UserRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);
        _fixture.PatientRepository
            .GetByIdAsync(patientId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _fixture.AppointmentRepository
            .GetByIdAsync(slotId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        _fixture.AppointmentRepository
            .TryCommitAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.ReservePortalAsync(userId, slotId, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await _fixture.AppointmentRepository
            .Received(1)
            .TryCommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservePortalAsync_UserNotAuthenticated_ThrowsUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        _fixture.UserRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var sut = _fixture.CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.ReservePortalAsync(userId, slotId, null, CancellationToken.None));
    }

    [Fact]
    public async Task ReservePortalAsync_PatientResolutionFails_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        var user = new UserBuilder()
            .WithId(userId)
            .WithPatientId(null)
            .WithEmail("test@test.com")
            .AsPatient()
            .Build();

        _fixture.UserRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);
        _fixture.PatientRepository
            .GetByPortalIdentifierAsync(user.Identifier, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);
        _fixture.PatientRepository
            .GetByPortalIdentifierAsync(user.Email, Arg.Any<CancellationToken>())
            .Returns((Patient?)null);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.ReservePortalAsync(userId, slotId, null, CancellationToken.None));
        Assert.Contains("Prohibido", exception.Message);
    }

    #endregion

    #region CancelPortalAsync

    [Fact]
    public async Task CancelPortalAsync_OwnAppointment_CancelsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);

        var user = new UserBuilder()
            .WithId(userId)
            .WithPatientId(patientId)
            .AsPatient()
            .Build();

        var patient = new PatientBuilder()
            .WithId(patientId)
            .Active()
            .Build();

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(date)
            .WithHora(9, 0)
            .AsOcupado(patientId)
            .Build();

        _fixture.UserRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);
        _fixture.PatientRepository
            .GetByIdAsync(patientId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _fixture.AppointmentRepository
            .GetByIdAsync(slotId, Arg.Any<CancellationToken>())
            .Returns(appointment);
        _fixture.AppointmentRepository
            .TryCommitAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.CancelPortalAsync(userId, slotId, null, "Test cancellation", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await _fixture.AppointmentRepository
            .Received(1)
            .TryCommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelPortalAsync_NotOwnedAppointment_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var otherPatientId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var date = _fixture.GetFutureDateInArgentina(1);

        var user = new UserBuilder()
            .WithId(userId)
            .WithPatientId(patientId)
            .AsPatient()
            .Build();

        var patient = new PatientBuilder()
            .WithId(patientId)
            .Active()
            .Build();

        var appointment = new AppointmentBuilder()
            .WithId(slotId)
            .WithFecha(date)
            .WithHora(9, 0)
            .AsOcupado(otherPatientId)
            .Build();

        _fixture.UserRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);
        _fixture.PatientRepository
            .GetByIdAsync(patientId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _fixture.AppointmentRepository
            .GetByIdAsync(slotId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.CancelPortalAsync(userId, slotId, null, "Test cancellation", CancellationToken.None));
        Assert.Contains("Prohibido", exception.Message);
    }

    #endregion

    #region GenerateAsync

    [Fact]
    public async Task GenerateAsync_WithValidDate_GeneratesAppointments()
    {
        // Arrange
        var date = _fixture.GetFutureDateInArgentina(1);
        var cameraId = 1;

        var scheduleHours = new List<ScheduleHour>
        {
            new(1, "09:00", 1, true),
            new(2, "10:00", 2, true)
        };

        var cameras = new List<Camera>
        {
            new(cameraId, "Cámara Test", 2, true)
        };

        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(scheduleHours);
        _fixture.CameraRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(cameras);
        _fixture.AppointmentRepository
            .GetByDateAsync(date, Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.UnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GenerateAsync(date, CancellationToken.None);

        // Assert
        Assert.Equal(4, result); // 2 hours × 1 camera × 2 capacity = 4 slots
        await _fixture.ScheduleRepository
            .Received(4)
            .AddAsync(Arg.Any<Schedule>(), Arg.Any<CancellationToken>());
        await _fixture.AppointmentRepository
            .Received(4)
            .AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_WithInvalidDate_ThrowsValidationException()
    {
        // Arrange
        var date = default(DateOnly);

        var sut = _fixture.CreateSut();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => sut.GenerateAsync(date, CancellationToken.None));
        Assert.Contains("Fecha invalida", exception.Message);
    }

    #endregion

    #region RepairRangeAsync

    [Fact]
    public async Task RepairRangeAsync_WithValidRange_RepairsAppointments()
    {
        // Arrange
        var startDate = _fixture.GetFutureDateInArgentina(1);
        var endDate = startDate.AddDays(2);
        var cameraId = 1;

        var scheduleHours = new List<ScheduleHour>
        {
            new(1, "09:00", 1, true),
            new(2, "10:00", 2, true)
        };

        var cameras = new List<Camera>
        {
            new(cameraId, "Cámara Test", 2, true)
        };

        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(scheduleHours);
        _fixture.CameraRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(cameras);
        _fixture.AppointmentRepository
            .GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.UnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.RepairRangeAsync(startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Equal(12, result); // 3 days × 2 hours × 1 camera × 2 capacity = 12 slots
    }

    #endregion
}
