using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Appointments;

/// <summary>
/// Unit tests for AppointmentsService query methods.
/// Tests read-only operations that don't modify state.
/// </summary>
public sealed class AppointmentsServiceQueryTests : IClassFixture<AppointmentsServiceTestFixture>
{
    private readonly AppointmentsServiceTestFixture _fixture;

    public AppointmentsServiceQueryTests(AppointmentsServiceTestFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    #region GetByDateAsync

    [Fact]
    public async Task GetByDateAsync_WithExistingAppointments_ReturnsAppointments()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);
        var patientId = Guid.NewGuid();
        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(9, 0)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(10, 0)
                .AsOcupado(patientId)
                .Build()
        };

        _fixture.AppointmentRepository
            .GetByDateAsync(date, Arg.Any<CancellationToken>())
            .Returns(appointments);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetByDateAsync(date, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        await _fixture.AppointmentRepository
            .Received(1)
            .GetByDateAsync(date, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByDateAsync_WithNullDate_ReturnsEmptyList()
    {
        // Arrange
        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetByDateAsync(null, CancellationToken.None);

        // Assert
        Assert.Empty(result);
        await _fixture.AppointmentRepository
            .DidNotReceiveWithAnyArgs()
            .GetByDateAsync(default, default);
    }

    [Fact]
    public async Task GetByDateAsync_WithNoAppointments_ReturnsEmptyList()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);

        _fixture.AppointmentRepository
            .GetByDateAsync(date, Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetByDateAsync(date, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetByRangeAsync

    [Fact]
    public async Task GetByRangeAsync_WithAppointmentsInRange_ReturnsGroupedAppointments()
    {
        // Arrange
        var startDate = new DateOnly(2026, 5, 15);
        var endDate = new DateOnly(2026, 5, 17);
        var patientId = Guid.NewGuid();

        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(startDate)
                .WithHora(9, 0)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(startDate.AddDays(1))
                .WithHora(10, 0)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(endDate)
                .WithHora(11, 0)
                .AsOcupado(patientId)
                .Build()
        };

        _fixture.AppointmentRepository
            .GetByRangeAsync(startDate, endDate, null, null, Arg.Any<CancellationToken>())
            .Returns(appointments);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetByRangeAsync(startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        await _fixture.AppointmentRepository
            .Received(1)
            .GetByRangeAsync(startDate, endDate, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByRangeAsync_WithNoAppointmentsInRange_ReturnsEmptyList()
    {
        // Arrange
        var startDate = new DateOnly(2026, 5, 15);
        var endDate = new DateOnly(2026, 5, 17);

        _fixture.AppointmentRepository
            .GetByRangeAsync(startDate, endDate, null, null, Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetByRangeAsync(startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetActivosByPacienteAsync

    [Fact]
    public async Task GetActivosByPacienteAsync_WithPatientAppointments_ReturnsAppointments()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var today = _fixture.GetTodayInArgentina();
        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(today.AddDays(1))
                .WithHora(9, 0)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithFecha(today.AddDays(2))
                .WithHora(10, 0)
                .AsOcupado(patientId)
                .Build()
        };

        _fixture.AppointmentRepository
            .GetActivosByPacienteAsync(patientId, today, Arg.Any<CancellationToken>())
            .Returns(appointments);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetActivosByPacienteAsync(patientId, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        await _fixture.AppointmentRepository
            .Received(1)
            .GetActivosByPacienteAsync(patientId, today, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetActivosByPacienteAsync_WithNoAppointments_ReturnsEmptyList()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var today = _fixture.GetTodayInArgentina();

        _fixture.AppointmentRepository
            .GetActivosByPacienteAsync(patientId, today, Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetActivosByPacienteAsync(patientId, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetSlotsByTandaAsync

    [Fact]
    public async Task GetSlotsByTandaAsync_WithTandaId_ReturnsSlots()
    {
        // Arrange
        var tandaId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithTanda(tandaId)
                .WithHora(9, 0)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithTanda(tandaId)
                .WithHora(9, 0)
                .AsOcupado(patientId)
                .Build()
        };

        _fixture.AppointmentRepository
            .GetByTandaIdAsync(tandaId, Arg.Any<CancellationToken>())
            .Returns(appointments);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetSlotsByTandaAsync(tandaId, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        await _fixture.AppointmentRepository
            .Received(1)
            .GetByTandaIdAsync(tandaId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetActiveSlotsByTandaAsync

    [Fact]
    public async Task GetActiveSlotsByTandaAsync_WithTandaId_ReturnsOnlyOccupiedSlots()
    {
        // Arrange
        var tandaId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 5, 15);

        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithTanda(tandaId)
                .WithFecha(fecha)
                .WithHora(9, 0)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithTanda(tandaId)
                .WithFecha(fecha)
                .WithHora(10, 0)
                .AsOcupado(patientId)
                .Build(),
            new AppointmentBuilder()
                .WithTanda(tandaId)
                .WithFecha(fecha)
                .WithHora(11, 0)
                .AsCancelado()
                .Build(),
            new AppointmentBuilder()
                .WithTanda(tandaId)
                .WithFecha(fecha)
                .WithHora(12, 0)
                .AsLibre()
                .Build()
        };

        _fixture.AppointmentRepository
            .GetByTandaIdAsync(tandaId, Arg.Any<CancellationToken>())
            .Returns(appointments);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetActiveSlotsByTandaAsync(tandaId, CancellationToken.None);

        // Assert
        // Only Ocupado slots are considered "active"
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region GetDisponiblesPortalByDateAsync

    [Fact]
    public async Task GetDisponiblesPortalByDateAsync_WithUserAndSlots_ReturnsAvailableSlots()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var date = new DateOnly(2026, 5, 15);

        var user = new UserBuilder()
            .WithId(userId)
            .WithPatientId(patientId)
            .Build();

        var patient = new PatientBuilder()
            .WithId(patientId)
            .Active()
            .Build();

        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(9, 0)
                .AsLibre()
                .Build(),
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(10, 0)
                .AsOcupado(patientId)
                .Build()
        };

        _fixture.UserRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);
        _fixture.PatientRepository
            .GetByIdAsync(patientId, Arg.Any<CancellationToken>())
            .Returns(patient);
        _fixture.AppointmentRepository
            .GetByDateAsync(date, Arg.Any<CancellationToken>())
            .Returns(appointments);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetDisponiblesPortalByDateAsync(userId, date, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count); // Both patient's own and libre slots
    }

    #endregion

    #region GetTandaAvailabilityAsync

    [Fact]
    public async Task GetTandaAvailabilityAsync_WithAppointments_ReturnsAvailabilitySummary()
    {
        // Arrange
        var startDate = new DateOnly(2026, 5, 15);
        var endDate = new DateOnly(2026, 5, 15);

        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(startDate)
                .WithHora(9, 0)
                .AsLibre()
                .Build(),
            new AppointmentBuilder()
                .WithFecha(startDate)
                .WithHora(9, 0)
                .AsOcupado(Guid.NewGuid())
                .Build(),
            new AppointmentBuilder()
                .WithFecha(startDate)
                .WithHora(10, 0)
                .AsLibre()
                .Build()
        };

        var cameras = new List<Camera>
        {
            new(1, "Camera A", 4, true)
        };

        _fixture.AppointmentRepository
            .GetByRangeAsync(startDate, endDate, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(appointments);
        _fixture.CameraRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(cameras);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ScheduleHour(1, "09:00", 1, true),
                new ScheduleHour(2, "10:00", 2, true)
            });

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetTandaAvailabilityAsync(startDate, endDate, null, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
    }

    #endregion

    #region GetEnrichedByDateAsync

    [Fact]
    public async Task GetEnrichedByDateAsync_WithAppointments_ReturnsEnrichedSummaries()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);
        var patientId = Guid.NewGuid();

        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithFecha(date)
                .WithHora(9, 0)
                .AsOcupado(patientId)
                .Build()
        };

        _fixture.AppointmentRepository
            .GetByRangeAsync(date, date, 0, int.MaxValue, Arg.Any<CancellationToken>())
            .Returns(appointments);
        _fixture.AppointmentRepository
            .CountByRangeAsync(date, date, Arg.Any<CancellationToken>())
            .Returns(1);
        _fixture.PatientRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.MedicoRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.ObraSocialRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _fixture.ScheduleHourRepository
            .GetAsync(Arg.Any<CancellationToken>())
            .Returns(ScheduleHourBuilder.CreateStandardHours());

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetEnrichedByDateAsync(date, CancellationToken.None);

        // Assert
        Assert.Single(result);
    }

    #endregion

    #region GetBlockHistoryAsync

    [Fact]
    public async Task GetBlockHistoryAsync_WithHistoryEntries_ReturnsHistory()
    {
        // Arrange
        var date = new DateOnly(2026, 5, 15);
        var hora = new TimeOnly(9, 0);
        var slotId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var historyEntries = new List<BlockHistory>
        {
            new(new BlockHistoryCreateParams(
                Guid.NewGuid(),
                date,
                hora,
                1,
                slotId,
                1,
                "bloqueado",
                patientId,
                actorId,
                null,
                false,
                "particular",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                null,
                null,
                null,
                null))
        };

        _fixture.BlockHistoryRepository
            .GetByBlockAsync(date, hora, 1, Arg.Any<CancellationToken>())
            .Returns(historyEntries);

        var sut = _fixture.CreateSut();

        // Act
        var result = await sut.GetBlockHistoryAsync(date, hora, 1, CancellationToken.None);

        // Assert
        Assert.Single(result);
        await _fixture.BlockHistoryRepository
            .Received(1)
            .GetByBlockAsync(date, hora, 1, Arg.Any<CancellationToken>());
    }

    #endregion
}
