using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Application.Features.WhatsApp;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Appointments;

public sealed class TurnoEnrichmentAssemblyTests
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
        return new AppointmentsService(
            _appointmentRepository,
            _scheduleRepository,
            _userRepository,
            _scheduleHourRepository,
            _cameraRepository,
            _patientRepository,
            _medicoRepository,
            _referenteRepository,
            _obraSocialRepository,
            _blockHistoryRepository,
            _whatsappService,
            _unitOfWork,
            _idempotencyStore,
            _clock);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_AssemblyProducesCorrectNestedObjects()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var pacienteId = Guid.NewGuid();
        var medicoId = 1;
        var referenteId = 2;
        var obraSocialId = 3;
        var camaraId = 4;

        var appointment = CreateAppointment(fecha, new TimeOnly(9, 0), 1, camaraId, AppointmentStatus.Ocupado, pacienteId);
        typeof(Appointment).GetProperty(nameof(Appointment.MedicoId))?.SetValue(appointment, medicoId);
        typeof(Appointment).GetProperty(nameof(Appointment.ReferenteId))?.SetValue(appointment, referenteId);
        typeof(Appointment).GetProperty(nameof(Appointment.ObraSocialId))?.SetValue(appointment, obraSocialId);

        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new ScheduleHour(1, "09:00", 1, true)]);

        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, 100, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(1);

        var patient = new Patient(pacienteId, "Juan Pérez", "123456789", "DNI123", null, 1, false);
        typeof(Patient).GetProperty(nameof(Patient.ObraSocialId))?.SetValue(patient, obraSocialId);
        _patientRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([patient]);

        var medico = new Medico("Dr. García", 1);
        typeof(Medico).GetProperty(nameof(Medico.Id))?.SetValue(medico, medicoId);
        _medicoRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([medico]);

        var referente = new Referente("Dr. López", "derivante", 1);
        typeof(Referente).GetProperty(nameof(Referente.Id))?.SetValue(referente, referenteId);
        _referenteRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([referente]);

        var obraSocial = new ObraSocial(obraSocialId, "OSDE", true, true, 1, "OS");
        _obraSocialRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([obraSocial]);

        _cameraRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new Camera(camaraId, "Cámara 1", 4, true)]);

        _appointmentRepository.GetBlockHistoryByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, 0, 100, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
        var item = result.Items.First();

        // Verify flat fields
        Assert.Equal(appointment.Id, item.Id);
        Assert.Equal(fecha, item.Fecha);
        Assert.Equal("ocupado", item.Estado);

        // Verify nested objects
        Assert.NotNull(item.Paciente);
        Assert.Equal(pacienteId, item.Paciente.Id);
        Assert.Equal("Juan Pérez", item.Paciente.Nombre);
        Assert.Equal(obraSocialId, item.Paciente.ObraSocialId);

        Assert.NotNull(item.Medico);
        Assert.Equal(medicoId, item.Medico.Id);
        Assert.Equal("Dr. García", item.Medico.Nombre);
        Assert.True(item.Medico.Activo);

        Assert.NotNull(item.Referente);
        Assert.Equal(referenteId, item.Referente.Id);
        Assert.Equal("Dr. López", item.Referente.Nombre);
        Assert.Equal("derivante", item.Referente.Tipo);
        Assert.True(item.Referente.Activo);

        Assert.NotNull(item.ObraSocial);
        Assert.Equal(obraSocialId, item.ObraSocial.Id);
        Assert.Equal("OSDE", item.ObraSocial.Nombre);
        Assert.True(item.ObraSocial.Activa);
        Assert.True(item.ObraSocial.TieneConvenio);

        Assert.NotNull(item.Camara);
        Assert.Equal(camaraId, item.Camara.Id);
        Assert.Equal("Cámara 1", item.Camara.Nombre);
        Assert.Equal(4, item.Camara.Capacidad);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_NullLookups_ProducesNullNestedObjects()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var appointment = CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre);

        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new ScheduleHour(1, "09:00", 1, true)]);

        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, 100, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(1);

        // Return empty collections for all batch lookups
        _patientRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _medicoRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _referenteRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _obraSocialRepository.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _appointmentRepository.GetBlockHistoryByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, 0, 100, CancellationToken.None);

        // Assert — no IDs set on appointment, all nested lookups should be null
        var item = result.Items.First();

        Assert.Null(item.PacienteId);
        Assert.Null(item.MedicoId);
        Assert.Null(item.ReferenteId);
        Assert.Null(item.ObraSocialId);
        // CamaraId is set in CreateAppointment — only nested Camara lookup should be null
        Assert.Null(item.Camara);
        Assert.Null(item.Paciente);
        Assert.Null(item.Medico);
        Assert.Null(item.Referente);
        Assert.Null(item.ObraSocial);
        Assert.Null(item.Camara);
        Assert.Null(item.ObraSocialValidadaPorPerfil);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_PaginationParams_PropagateCorrectly()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var offset = 10;
        var limit = 25;

        _appointmentRepository.GetByRangeAsync(fecha, fecha, offset, limit, Arg.Any<CancellationToken>())
            .Returns([]);
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(0);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, offset, limit, CancellationToken.None);

        // Assert — verify the repository was called with the correct offset/limit
        await _appointmentRepository.Received(1).GetByRangeAsync(fecha, fecha, offset, limit, Arg.Any<CancellationToken>());
        // CountByRangeAsync is NOT called when GetByRangeAsync returns empty (early return)

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_BlockHistoryLatestValidation_SelectedCorrectly()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var slotId = Guid.NewGuid();
        var appointment = CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Ocupado);
        typeof(Appointment).GetProperty(nameof(Appointment.Id))?.SetValue(appointment, slotId);

        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new ScheduleHour(1, "09:00", 1, true)]);

        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, 100, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(1);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        var validatingUserId = Guid.NewGuid();
        var blockHistories = new[]
        {
            // Older validation — should NOT be selected
            new BlockHistory(
                Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, slotId, 1,
                "ocupado", null, null, null, false, "particular",
                null, null, null, new DateTimeOffset(2024, 5, 2, 12, 0, 0, TimeSpan.Zero),
                null, false, null, null, null, null),
            // Newer validation — SHOULD be selected
            new BlockHistory(
                Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, slotId, 1,
                "validado", null, null, null, false, "particular",
                null, null, validatingUserId, new DateTimeOffset(2024, 5, 2, 14, 0, 0, TimeSpan.Zero),
                null, false, null, null, null, null),
        };

        _appointmentRepository.GetBlockHistoryByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(blockHistories);

        var validatingUser = new User(validatingUserId, "validator", "v@t.com", "hash", true, true, null, "Validador Pérez");
        _userRepository.GetBasicByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([validatingUser]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, 0, 100, CancellationToken.None);

        // Assert
        var item = result.Items.First();
        Assert.Equal(validatingUserId, item.ObraSocialValidadaPor);
        Assert.Equal(new DateTimeOffset(2024, 5, 2, 14, 0, 0, TimeSpan.Zero), item.ObraSocialValidadaAt);

        Assert.NotNull(item.ObraSocialValidadaPorPerfil);
        Assert.Equal(validatingUserId, item.ObraSocialValidadaPorPerfil.Id);
        Assert.Equal("Validador Pérez", item.ObraSocialValidadaPorPerfil.Nombre);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_WhenNoBlockHistory_ValidationFieldsAreNull()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var slotId = Guid.NewGuid();
        var appointment = CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Ocupado);
        typeof(Appointment).GetProperty(nameof(Appointment.Id))?.SetValue(appointment, slotId);

        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new ScheduleHour(1, "09:00", 1, true)]);

        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, 100, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(1);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        _appointmentRepository.GetBlockHistoryByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, 0, 100, CancellationToken.None);

        // Assert
        var item = result.Items.First();
        Assert.Null(item.ObraSocialValidadaPor);
        Assert.Null(item.ObraSocialValidadaAt);
        Assert.Null(item.ObraSocialValidadaPorPerfil);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_EmptyRange_ReturnsEmptyResult()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);

        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, 100, Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, 0, 100, CancellationToken.None);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_DateWithNoAppointments_ReturnsEmpty()
    {
        // Arrange
        var fechaWithData = new DateOnly(2024, 5, 2);
        var fechaEmpty = new DateOnly(2024, 5, 3);

        // Appointments exist for May 2
        _appointmentRepository.GetByRangeAsync(fechaWithData, fechaWithData, 0, 100, Arg.Any<CancellationToken>())
            .Returns([CreateAppointment(fechaWithData, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre)]);
        _appointmentRepository.CountByRangeAsync(fechaWithData, fechaWithData, Arg.Any<CancellationToken>())
            .Returns(1);
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new ScheduleHour(1, "09:00", 1, true)]);

        // But query for May 3 returns nothing
        _appointmentRepository.GetByRangeAsync(fechaEmpty, fechaEmpty, 0, 100, Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fechaEmpty, fechaEmpty, 0, 100, CancellationToken.None);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_InactiveScheduleHour_FiltersOutAppointment()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var appointment = CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre);

        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, 100, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(1);

        // ScheduleHour for "09:00" is INACTIVE → filtered out
        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new ScheduleHour(1, "09:00", 1, false)]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, 0, 100, CancellationToken.None);

        // Assert — appointment filtered out by active hours filter
        Assert.Empty(result.Items);
        // Total is the raw count from the repository (pre-filter)
        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_InactiveCamera_StillIncluded()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var camaraId = 1;
        var appointment = CreateAppointment(fecha, new TimeOnly(9, 0), 1, camaraId, AppointmentStatus.Libre);

        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new ScheduleHour(1, "09:00", 1, true)]);

        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, 100, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(1);

        _cameraRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new Camera(camaraId, "Cámara Inactiva", 4, false)]); // Camera is inactive

        _appointmentRepository.GetBlockHistoryByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, 0, 100, CancellationToken.None);

        // Assert — appointment IS included, but Camara is null (filtered from dictionary)
        Assert.Single(result.Items);
        var item = result.Items.First();
        Assert.Equal(camaraId, item.CamaraId);
        Assert.Null(item.Camara);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_MultipleAppointments_CorrectCount()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);

        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([
                new ScheduleHour(1, "09:00", 1, true),
                new ScheduleHour(2, "10:00", 2, true),
                new ScheduleHour(3, "11:00", 3, true)
            ]);

        var appointments = new[]
        {
            CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(10, 0), 1, 1, AppointmentStatus.Libre),
            CreateAppointment(fecha, new TimeOnly(11, 0), 1, 1, AppointmentStatus.Libre),
        };

        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, 100, Arg.Any<CancellationToken>())
            .Returns(appointments);
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(3);

        _appointmentRepository.GetBlockHistoryByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, 0, 100, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task GetEnrichedByRangeAsync_PaginationExactBoundary_ReturnsOneItem()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);

        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([
                new ScheduleHour(1, "09:00", 1, true),
                new ScheduleHour(2, "10:00", 2, true),
                new ScheduleHour(3, "11:00", 3, true)
            ]);

        // Repository returns only 1 item for offset=0, limit=1
        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, 1, Arg.Any<CancellationToken>())
            .Returns([
                CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Libre)
            ]);
        // Count shows 3 total
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(3);

        _appointmentRepository.GetBlockHistoryByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByRangeAsync(fecha, fecha, 0, 1, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task GetEnrichedByDateAsync_ReturnsCorrectData()
    {
        // Arrange
        var fecha = new DateOnly(2024, 5, 2);
        var pacienteId = Guid.NewGuid();
        var appointment = CreateAppointment(fecha, new TimeOnly(9, 0), 1, 1, AppointmentStatus.Ocupado, pacienteId);

        _scheduleHourRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new ScheduleHour(1, "09:00", 1, true)]);

        // GetEnrichedByDateAsync delegates to GetEnrichedByRangeAsync with offset=0, limit=int.MaxValue
        _appointmentRepository.GetByRangeAsync(fecha, fecha, 0, int.MaxValue, Arg.Any<CancellationToken>())
            .Returns([appointment]);
        _appointmentRepository.CountByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns(1);

        _patientRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([new Patient(pacienteId, "Juan Pérez", "123456789", "DNI123", null, 1, false)]);

        _appointmentRepository.GetBlockHistoryByRangeAsync(fecha, fecha, Arg.Any<CancellationToken>())
            .Returns([]);

        var service = CreateService();

        // Act
        var result = await service.GetEnrichedByDateAsync(fecha, CancellationToken.None);

        // Assert
        Assert.Single(result);
        var item = result.First();
        Assert.Equal(fecha, item.Fecha);
        Assert.Equal("ocupado", item.Estado);
        Assert.NotNull(item.Paciente);
        Assert.Equal(pacienteId, item.Paciente.Id);
        Assert.Equal("Juan Pérez", item.Paciente.Nombre);
    }

    private static Appointment CreateAppointment(DateOnly fecha, TimeOnly hora, int lugar, int camaraId, AppointmentStatus status, Guid? patientId = null)
    {
        var appointment = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, hora, lugar, camaraId);
        typeof(Appointment).GetProperty(nameof(Appointment.Status))?.SetValue(appointment, status);
        if (patientId.HasValue)
        {
            typeof(Appointment).GetProperty(nameof(Appointment.PatientId))?.SetValue(appointment, patientId.Value);
        }

        return appointment;
    }
}
