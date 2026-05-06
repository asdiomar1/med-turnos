using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Application.Features.WhatsApp;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Appointments;

public sealed class AppointmentsServiceAggregationTests
{
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly IScheduleRepository _scheduleRepository = Substitute.For<IScheduleRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IScheduleHourRepository _scheduleHourRepository = Substitute.For<IScheduleHourRepository>();
    private readonly ICameraRepository _cameraRepository = Substitute.For<ICameraRepository>();
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly IMedicoRepository _medicoRepository = Substitute.For<IMedicoRepository>();
    private readonly IReferenteRepository _referenteRepository = Substitute.For<IReferenteRepository>();
    private readonly IObraSocialRepository _obraSocialRepository = Substitute.For<IObraSocialRepository>();
    private readonly IBlockHistoryRepository _blockHistoryRepository = Substitute.For<IBlockHistoryRepository>();
    private readonly IWhatsappService _whatsappService = Substitute.For<IWhatsappService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IIdempotencyStore _idempotencyStore = Substitute.For<IIdempotencyStore>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private AppointmentsService CreateService()
    {
        var dataAccess = new AppointmentsDataAccessDependencies(
            _appointmentRepository,
            _scheduleRepository,
            _userRepository,
            _scheduleHourRepository,
            _cameraRepository,
            _patientRepository,
            _medicoRepository,
            _referenteRepository,
            _obraSocialRepository,
            _blockHistoryRepository);
        var runtime = new AppointmentsRuntimeDependencies(
            _whatsappService,
            _unitOfWork,
            _idempotencyStore,
            _clock);
        return new AppointmentsService(
            dataAccess,
            runtime);
    }

    [Fact]
    public async Task GetTandaAvailabilityAggregatedAsync_GroupsByFechaHoraCamara_CreatesCorrectGroups()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var camara1 = new Camera(1, "Cámara A", 4, true);
        var camara2 = new Camera(2, "Cámara B", 4, true);

        var appointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(9, 0), 2, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 2, AppointmentStatus.Ocupado), // Same time, different camera
            CreateAppointment(fecha, new TimeOnly(10, 0), 1, 1, AppointmentStatus.Libre),
        };

        _appointmentRepository.GetByRangeAsync(fecha, fecha, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(appointments);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { camara1, camara2 });
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new ScheduleHour(1, "09:00", 1, true),
            new ScheduleHour(2, "10:00", 2, true)
        });

        var service = CreateService();

        // Act
        var result = await service.GetTandaAvailabilityAggregatedAsync(fecha, fecha, null, CancellationToken.None);

        // Assert — cross-join: 2 hours × 2 cameras = 4 combinations
        Assert.Equal(4, result.Count); // (09:00,C1), (09:00,C2), (10:00,C1), (10:00,C2)
        var group1 = result.First(r => r.Hora == new TimeOnly(9, 0) && r.CamaraId == 1);
        Assert.Equal(2, group1.LibresCount); // 2 libres
        Assert.Equal(4, group1.Capacidad);
    }

    [Fact]
    public async Task GetTandaAvailabilityAggregatedAsync_CountsLibresCorrectly_IncludesLibreAndCancelado()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var camara1 = new Camera(1, "Cámara A", 4, true);

        var appointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(9, 0), 2, 1, AppointmentStatus.Cancelado),
            CreateAppointment(fecha, new TimeOnly(9, 0), 3, 1, AppointmentStatus.Ocupado),
            CreateAppointment(fecha, new TimeOnly(9, 0), 4, 1, AppointmentStatus.Apartado),
        };

        _appointmentRepository.GetByRangeAsync(fecha, fecha, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(appointments);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { camara1 });
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new ScheduleHour(1, "09:00", 1, true)
        });

        var service = CreateService();

        // Act
        var result = await service.GetTandaAvailabilityAggregatedAsync(fecha, fecha, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result.First().LibresCount); // Libre + Cancelado = 2
        Assert.True(result.First().TieneDisponibilidad);
        Assert.False(result.First().TieneBloqueCompletoPosible); // 2 != 4
    }

    [Fact]
    public async Task GetTandaAvailabilityAggregatedAsync_WhenAllSlotsAvailable_TieneBloqueCompletoPosibleTrue()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var camara1 = new Camera(1, "Cámara A", 4, true);

        var appointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(9, 0), 2, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(9, 0), 3, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(9, 0), 4, 1, AppointmentStatus.Libre),
        };

        _appointmentRepository.GetByRangeAsync(fecha, fecha, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(appointments);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { camara1 });
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new ScheduleHour(1, "09:00", 1, true)
        });

        var service = CreateService();

        // Act
        var result = await service.GetTandaAvailabilityAggregatedAsync(fecha, fecha, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(4, result.First().LibresCount);
        Assert.True(result.First().TieneBloqueCompletoPosible); // 4 == 4
    }

    [Fact]
    public async Task GetTandaAvailabilityAggregatedAsync_WhenNoLibres_TieneDisponibilidadFalse()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var camara1 = new Camera(1, "Cámara A", 4, true);

        var appointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Ocupado),
            CreateAppointment(fecha, new TimeOnly(9, 0), 2, 1, AppointmentStatus.Ocupado),
            CreateAppointment(fecha, new TimeOnly(9, 0), 3, 1, AppointmentStatus.Apartado),
            CreateAppointment(fecha, new TimeOnly(9, 0), 4, 1, AppointmentStatus.Ocupado),
        };

        _appointmentRepository.GetByRangeAsync(fecha, fecha, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(appointments);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { camara1 });
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new ScheduleHour(1, "09:00", 1, true)
        });

        var service = CreateService();

        // Act
        var result = await service.GetTandaAvailabilityAggregatedAsync(fecha, fecha, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(0, result.First().LibresCount);
        Assert.False(result.First().TieneDisponibilidad);
        Assert.False(result.First().TieneBloqueCompletoPosible);
    }

    [Fact]
    public async Task GetTandaAvailabilityAggregatedAsync_FiltersInactiveCameras()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var camara1 = new Camera(1, "Cámara A", 4, true);
        var camara2 = new Camera(2, "Cámara B", 4, false); // Inactive

        var appointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 2, AppointmentStatus.Libre), // Should be filtered
        };

        _appointmentRepository.GetByRangeAsync(fecha, fecha, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(appointments);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { camara1, camara2 });
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new ScheduleHour(1, "09:00", 1, true)
        });

        var service = CreateService();

        // Act
        var result = await service.GetTandaAvailabilityAggregatedAsync(fecha, fecha, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result.First().CamaraId); // Only active camera
    }

    [Fact]
    public async Task GetTandaAvailabilityAggregatedAsync_FiltersInactiveScheduleHours()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var camara1 = new Camera(1, "Cámara A", 4, true);

        var appointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(10, 0), 1, 1, AppointmentStatus.Libre), // Inactive hour
        };

        _appointmentRepository.GetByRangeAsync(fecha, fecha, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(appointments);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { camara1 });
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new ScheduleHour(1, "09:00", 1, true),
            new ScheduleHour(2, "10:00", 2, false) // Inactive
        });

        var service = CreateService();

        // Act
        var result = await service.GetTandaAvailabilityAggregatedAsync(fecha, fecha, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(new TimeOnly(9, 0), result.First().Hora); // Only active hour
    }

    [Fact]
    public async Task GetTandaAvailabilityAggregatedAsync_WithPatient_BlocksConsecutiveHours()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var patientId = Guid.NewGuid();
        var camara1 = new Camera(1, "Cámara A", 4, true);

        // Current appointments - note: patient has slot at 10:00 which is in patientAppointments
        var currentAppointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(10, 0), 1, 1, AppointmentStatus.Ocupado, patientId),
            CreateAppointment(fecha, new TimeOnly(11, 0), 1, 1, AppointmentStatus.Libre),
        };

        // Patient's occupied appointments for blocking calculation
        var patientAppointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(10, 0), 1, 1, AppointmentStatus.Ocupado, patientId),
        };

        _appointmentRepository.GetByRangeAsync(fecha, fecha, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(currentAppointments);
        _appointmentRepository.GetActivosByPacienteAsync(patientId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(patientAppointments);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { camara1 });
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new ScheduleHour(1, "09:00", 1, true),
            new ScheduleHour(2, "10:00", 2, true),
            new ScheduleHour(3, "11:00", 3, true)
        });

        var service = CreateService();

        // Act
        var result = await service.GetTandaAvailabilityAggregatedAsync(fecha, fecha, patientId, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);

        var nineAm = result.First(r => r.Hora == new TimeOnly(9, 0));
        var tenAm = result.First(r => r.Hora == new TimeOnly(10, 0));
        var elevenAm = result.First(r => r.Hora == new TimeOnly(11, 0));

        // Debug: Print all results
        var resultInfo = string.Join(", ", result.Select(r => $"{r.Hora}: blocked={r.BloqueadoPorPaciente}"));

        // Verify we have the 10:00 slot
        Assert.NotNull(tenAm);

        // Check blocking logic — paciente has slot at 10:00, so blocks 09:00, 10:00, 11:00
        Assert.True(nineAm.BloqueadoPorPaciente, $"9:00 should be blocked (H-1 of 10:00). Results: {resultInfo}");
        Assert.True(tenAm.BloqueadoPorPaciente, $"10:00 should be blocked (hora ocupada). Results: {resultInfo}");
        Assert.True(elevenAm.BloqueadoPorPaciente, $"11:00 should be blocked (H+1 of 10:00). Results: {resultInfo}");
    }

    [Fact]
    public async Task GetTandaAvailabilityAggregatedAsync_WithPatient_DoesNotBlockDifferentDates()
    {
        // Arrange
        var fecha1 = new DateOnly(2024, 5, 2);
        var fecha2 = new DateOnly(2024, 5, 3);
        var patientId = Guid.NewGuid();
        var camara1 = new Camera(1, "Cámara A", 4, true);

        // Appointments spanning two days
        var currentAppointments = new[]
        {
            CreateAppointment(fecha1, new TimeOnly(23, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha2, new TimeOnly(0, 0), 1, 1, AppointmentStatus.Libre),
        };

        // Patient has appointment at 23:00 on day 1
        var patientAppointments = new[]
        {
            CreateAppointment(fecha1, new TimeOnly(23, 0), 1, 1, AppointmentStatus.Ocupado, patientId),
        };

        _appointmentRepository.GetByRangeAsync(fecha1, fecha2, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(currentAppointments);
        _appointmentRepository.GetActivosByPacienteAsync(patientId, fecha1, Arg.Any<CancellationToken>())
            .Returns(patientAppointments);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { camara1 });
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new ScheduleHour(1, "23:00", 1, true),
            new ScheduleHour(2, "00:00", 2, true)
        });

        var service = CreateService();

        // Act
        var result = await service.GetTandaAvailabilityAggregatedAsync(fecha1, fecha2, patientId, CancellationToken.None);

        // Assert
        var day1Slot = result.First(r => r.Fecha == fecha1 && r.Hora == new TimeOnly(23, 0));
        var day2Slot = result.First(r => r.Fecha == fecha2 && r.Hora == new TimeOnly(0, 0));

        // 23:00 on day 1 blocks 22:00 and 23:00 (hora ocupada itself), but NOT 00:00 on day 2
        Assert.True(day1Slot.BloqueadoPorPaciente); // 23:00 is the occupied hour → blocked
        Assert.False(day2Slot.BloqueadoPorPaciente); // 00:00 on day 2 = different date, not blocked
    }

    [Fact]
    public async Task GetTandaAvailabilityAggregatedAsync_NoPatient_AllBloqueadoPorPacienteFalse()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var camara1 = new Camera(1, "Cámara A", 4, true);

        var appointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(10, 0), 1, 1, AppointmentStatus.Libre),
        };

        _appointmentRepository.GetByRangeAsync(fecha, fecha, Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(appointments);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[] { camara1 });
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new ScheduleHour(1, "09:00", 1, true),
            new ScheduleHour(2, "10:00", 2, true)
        });

        var service = CreateService();

        // Act - no patientId provided
        var result = await service.GetTandaAvailabilityAggregatedAsync(fecha, fecha, null, CancellationToken.None);

        // Assert
        Assert.All(result, r => Assert.False(r.BloqueadoPorPaciente));
    }

    private static Appointment CreateAppointment(DateOnly fecha, TimeOnly hora, int lugar, int camaraId, AppointmentStatus status, Guid? patientId = null)
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, hora, lugar, camaraId);

        // Use reflection to set private properties for testing
        typeof(Appointment).GetProperty(nameof(Appointment.Status))?.SetValue(appointment, status);
        if (patientId.HasValue)
        {
            typeof(Appointment).GetProperty(nameof(Appointment.PatientId))?.SetValue(appointment, patientId.Value);
        }

        return appointment;
    }
}
