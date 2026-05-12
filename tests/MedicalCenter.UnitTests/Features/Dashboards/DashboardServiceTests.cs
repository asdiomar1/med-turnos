using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Features.Dashboards;
using MedicalCenter.Domain.Entities;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Dashboards;

public sealed class DashboardServiceTests
{
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();
    private readonly ICameraRepository _cameraRepository = Substitute.For<ICameraRepository>();
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();

    [Fact]
    public async Task GetResumenAsync_ReturnsOnlyPacientesHoyAndApartadosActivos()
    {
        var fecha = new DateOnly(2026, 5, 11);
        var ocupado = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, 1);
        ocupado.Reserve(Guid.NewGuid());

        var apartado = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(10, 0), 2, 1);
        apartado.Hold(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        _appointmentRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>())
            .Returns([ocupado, apartado]);

        var service = new DashboardService(_appointmentRepository, _cameraRepository, _patientRepository);

        var result = await service.GetResumenAsync(fecha, CancellationToken.None);

        Assert.Equal(1, result.PacientesHoy);
        Assert.Equal(1, result.ApartadosActivos);
    }

    [Fact]
    public async Task GetOcupacionAsync_IncludesCamerasWithoutAppointments()
    {
        var fecha = new DateOnly(2026, 5, 11);
        var ocupado = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, 1);
        ocupado.Reserve(Guid.NewGuid());

        _appointmentRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>())
            .Returns([ocupado]);

        _cameraRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([
                new Camera(1, "Multiplaza", 24, true),
                new Camera(2, "Individual", 8, true)
            ]);

        var service = new DashboardService(_appointmentRepository, _cameraRepository, _patientRepository);

        var result = await service.GetOcupacionAsync(fecha, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.CamaraId == 1 && item.Ocupados == 1 && item.PorcentajeOcupacion == 4);
        Assert.Contains(result, item => item.CamaraId == 2 && item.Ocupados == 0 && item.PorcentajeOcupacion == 0);
    }

    [Fact]
    public async Task GetOcupacionAsync_WhenCapacityIsZero_PercentageIsZero()
    {
        var fecha = new DateOnly(2026, 5, 11);
        var ocupado = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, 1);
        ocupado.Reserve(Guid.NewGuid());

        _appointmentRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>())
            .Returns([ocupado]);

        _cameraRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new Camera(1, "Sin Capacidad", 0, true)]);

        var service = new DashboardService(_appointmentRepository, _cameraRepository, _patientRepository);

        var result = await service.GetOcupacionAsync(fecha, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(1, item.Ocupados);
        Assert.Equal(0, item.PorcentajeOcupacion);
    }

    [Fact]
    public async Task GetAgendaAsync_ReturnsOperationalRowsWithStableOrder()
    {
        var fecha = new DateOnly(2026, 5, 11);
        var patientA = CreatePatient("Juan Pérez");
        var patientB = CreatePatient("Ana Gómez");

        var first = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 2, 2);
        first.Reserve(patientA.Id);

        var second = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, 1);
        second.Hold(patientB.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);

        _appointmentRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>())
            .Returns([first, second]);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new Camera(1, "Multiplaza", 24, true), new Camera(2, "Individual", 8, true)]);
        _patientRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([patientA, patientB]);

        var service = new DashboardService(_appointmentRepository, _cameraRepository, _patientRepository);

        var result = (await service.GetAgendaAsync(fecha, CancellationToken.None)).ToArray();

        Assert.Equal(2, result.Length);
        Assert.Equal(1, result[0].CamaraId);
        Assert.Equal(1, result[0].Lugar);
        Assert.Equal("apartado", result[0].Estado);
        Assert.Equal("Ana Gómez", result[0].NombrePaciente);
        Assert.Equal(2, result[1].CamaraId);
        Assert.Equal("asignado", result[1].Estado);
        Assert.Equal("Juan Pérez", result[1].NombrePaciente);
    }

    [Fact]
    public async Task GetAgendaAsync_ExcludesLegacyNonOperationalStatuses()
    {
        var fecha = new DateOnly(2026, 5, 11);
        var libre = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, 1);
        var reprogramado = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(10, 0), 1, 1);
        reprogramado.Reserve(Guid.NewGuid());
        reprogramado.Reschedule();

        _appointmentRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>())
            .Returns([libre, reprogramado]);
        _cameraRepository.GetAsync(Arg.Any<CancellationToken>())
            .Returns([new Camera(1, "Multiplaza", 24, true)]);

        var service = new DashboardService(_appointmentRepository, _cameraRepository, _patientRepository);

        var result = await service.GetAgendaAsync(fecha, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAlertasAsync_ReturnsUiContractShape()
    {
        var fecha = new DateOnly(2026, 5, 11);
        var ocupado = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, 1);
        ocupado.Reserve(Guid.NewGuid());
        var apartado = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(10, 0), 1, 1);
        apartado.Hold(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        _appointmentRepository.GetByDateAsync(fecha, Arg.Any<CancellationToken>())
            .Returns([ocupado, apartado]);

        var service = new DashboardService(_appointmentRepository, _cameraRepository, _patientRepository);
        var result = (await service.GetAlertasAsync(fecha, CancellationToken.None)).ToArray();

        Assert.Single(result);
        Assert.Equal("apartados_pendientes", result[0].Tipo);
        Assert.Equal("Apartados pendientes", result[0].Titulo);
        Assert.Equal("agenda", result[0].TargetTab);
    }

    [Fact]
    public async Task GetVolumenSemanalAsync_Returns7ContinuousDaysAndFillsMissingWithZero()
    {
        var fecha = new DateOnly(2026, 5, 11);
        var withinRange = fecha.AddDays(-2);
        var outsideRange = fecha.AddDays(-8);

        var ocupado = new Appointment(Guid.NewGuid(), Guid.NewGuid(), withinRange, new TimeOnly(9, 0), 1, 1);
        ocupado.Reserve(Guid.NewGuid());
        var libre = new Appointment(Guid.NewGuid(), Guid.NewGuid(), withinRange, new TimeOnly(10, 0), 2, 1);
        _appointmentRepository.GetByRangeAsync(fecha.AddDays(-6), fecha, null, null, Arg.Any<CancellationToken>())
            .Returns([ocupado, libre]);

        var service = new DashboardService(_appointmentRepository, _cameraRepository, _patientRepository);
        var result = (await service.GetVolumenSemanalAsync(fecha, CancellationToken.None)).ToArray();

        Assert.Equal(7, result.Length);
        Assert.Equal(fecha.AddDays(-6), result[0].Fecha);
        Assert.Equal(fecha, result[^1].Fecha);
        Assert.Contains(result, x => x.Fecha == withinRange && x.Ocupados == 1);
        Assert.Contains(result, x => x.Fecha == fecha.AddDays(-6) && x.Ocupados == 0);
        Assert.DoesNotContain(result, x => x.Fecha == outsideRange);
    }

    private static Patient CreatePatient(string name) =>
        new(Guid.NewGuid(), name, new PatientAdministrativeInfo("123", "999", "999", 1), new PatientPortalInfo(false));
}
