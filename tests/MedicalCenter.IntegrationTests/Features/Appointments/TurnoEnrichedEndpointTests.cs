using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MedicalCenter.IntegrationTests.Features.Appointments;

/// <summary>
/// Integration tests for enriched appointment endpoints using Testcontainers PostgreSQL.
/// Tests repository-level operations with a real database — the foundation of the
/// enrichment pipeline.
/// </summary>
public sealed class TurnoEnrichedEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly Random _rng = new();

    public TurnoEnrichedEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static int NextSeed() => _rng.Next(100_000, 999_999_999);

    private DbContextOptions<MedicalCenterDbContext> Options() =>
        new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;

    [Fact]
    public async Task GetByRangeAsync_WithRealDb_ReturnsAppointmentsWithRelatedData()
    {
        // Arrange — each test uses a unique date + unique IDs to isolate data
        var seed = NextSeed();
        var options = Options();
        var fecha = new DateOnly(2025, 6, seed % 27 + 1); // June
        var camaraId = seed + 1000;
        var scheduleHourId = seed + 500_000;
        var pacienteId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();

        await using (var seedCtx = new MedicalCenterDbContext(options))
        {
            await seedCtx.Database.EnsureCreatedAsync();
        seedCtx.Patients.Add(new Patient(
            pacienteId,
            "María García",
            new PatientAdministrativeInfo("1155551234", "DNI40123456", null, 1),
            new PatientPortalInfo(false)));
            seedCtx.Cameras.Add(new Camera(camaraId, "Cámara Central", 4, true));
            seedCtx.ScheduleHours.Add(new ScheduleHour(scheduleHourId, SeedToHour(seed), seed, true));
            var appt = new Appointment(appointmentId, scheduleId, fecha, Hora(seed), 1, camaraId);
            appt.Reserve(pacienteId);
            seedCtx.Appointments.Add(appt);
            await seedCtx.SaveChangesAsync();
        }

        // Act — query repositories
        await using var queryCtx = new MedicalCenterDbContext(options);
        var appointmentRepo = new AppointmentRepository(queryCtx, NullLogger<AppointmentRepository>.Instance);
        var cameraRepo = new CameraRepository(queryCtx);
        var patientRepo = new PatientRepository(queryCtx);

        var appointments = await appointmentRepo.GetByRangeAsync(fecha, fecha, 0, 100, CancellationToken.None);
        var total = await appointmentRepo.CountByRangeAsync(fecha, fecha, CancellationToken.None);
        var cameras = (await cameraRepo.GetAsync(CancellationToken.None))
            .Where(c => c.Activa)
            .ToDictionary(c => c.Id);
        var patientIds = appointments
            .Where(a => a.PatientId.HasValue)
            .Select(a => a.PatientId!.Value)
            .Distinct()
            .ToArray();
        var patients = patientIds.Length > 0
            ? (await patientRepo.GetByIdsAsync(patientIds, CancellationToken.None)).ToDictionary(p => p.Id)
            : [];

        // Assert
        Assert.Single(appointments);
        Assert.Equal(1, total);

        var appointment = appointments.First();
        Assert.Equal(appointmentId, appointment.Id);
        Assert.Equal(fecha, appointment.Fecha);
        Assert.Equal(camaraId, appointment.CameraId);
        Assert.Equal(AppointmentStatus.Ocupado, appointment.Status);

        // Patient loaded correctly via batch query
        Assert.Contains(pacienteId, patients);
        Assert.Equal("María García", patients[pacienteId].Nombre);

        // Camera loaded correctly
        Assert.Contains(camaraId, cameras);
        Assert.Equal("Cámara Central", cameras[camaraId].Nombre);
        Assert.Equal(4, cameras[camaraId].Capacidad);
    }

    [Fact]
    public async Task GetByRangeAsync_Pagination_ReturnsCorrectSlice()
    {
        // Arrange
        var seed = NextSeed();
        var options = Options();
        var fecha = new DateOnly(2025, 7, seed % 28 + 1); // July (different month)
        var camaraId = seed + 2000;

        await using (var seedCtx = new MedicalCenterDbContext(options))
        {
            await seedCtx.Database.EnsureCreatedAsync();
            seedCtx.Appointments.Add(new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1, camaraId));
            seedCtx.Appointments.Add(new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(10, 0), 1, camaraId));
            seedCtx.Appointments.Add(new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(11, 0), 1, camaraId));
            await seedCtx.SaveChangesAsync();
        }

        // Act
        await using var queryCtx = new MedicalCenterDbContext(options);
        var appointmentRepo = new AppointmentRepository(queryCtx, NullLogger<AppointmentRepository>.Instance);

        var appointments = await appointmentRepo.GetByRangeAsync(fecha, fecha, 0, 2, CancellationToken.None);
        var total = await appointmentRepo.CountByRangeAsync(fecha, fecha, CancellationToken.None);

        // Assert
        Assert.Equal(2, appointments.Count);
        Assert.Equal(3, total);
    }

    [Fact]
    public async Task GetByRangeAsync_EmptyRange_ReturnsEmptyResult()
    {
        // Arrange
        var seed = NextSeed();
        var options = Options();
        var fecha = new DateOnly(2025, 8, seed % 28 + 1); // August (different month)

        await using (var ctx = new MedicalCenterDbContext(options))
        {
            await ctx.Database.EnsureCreatedAsync();
            // No appointments seeded
        }

        // Act
        await using var queryCtx = new MedicalCenterDbContext(options);
        var appointmentRepo = new AppointmentRepository(queryCtx, NullLogger<AppointmentRepository>.Instance);

        var appointments = await appointmentRepo.GetByRangeAsync(fecha, fecha, 0, 10, CancellationToken.None);
        var total = await appointmentRepo.CountByRangeAsync(fecha, fecha, CancellationToken.None);

        // Assert
        Assert.Empty(appointments);
        Assert.Equal(0, total);
    }

    // Helper: generate unique hour string from seed to avoid DB constraint collisions
    private static string SeedToHour(int seed) => $"{seed % 24:D2}:{seed % 60:D2}";

    private static TimeOnly Hora(int seed) => new(seed % 24, seed % 60);
}
