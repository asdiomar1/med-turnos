using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MedicalCenter.IntegrationTests.Persistence;

public sealed class AppointmentRepositoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AppointmentRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private DbContextOptions<MedicalCenterDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;

    private static AppointmentRepository MakeRepo(MedicalCenterDbContext ctx) =>
        new(ctx, NullLogger<AppointmentRepository>.Instance);

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingAppointment_ReturnsIt()
    {
        var id = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Appointments.Add(new Appointment(id, Guid.NewGuid(), new DateOnly(2031, 1, 1), new TimeOnly(9, 0), 1));
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var result = await MakeRepo(ctx).GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    // ── GetByIdsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdsAsync_EmptyInput_ReturnsEmpty()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var result = await MakeRepo(ctx).GetByIdsAsync([], CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdsAsync_InputContainsOnlyEmptyGuid_ReturnsEmpty()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var result = await MakeRepo(ctx).GetByIdsAsync([Guid.Empty, Guid.Empty], CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdsAsync_ValidIds_ReturnsMatchingAppointments()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Appointments.AddRange(
                new Appointment(id1, Guid.NewGuid(), new DateOnly(2031, 1, 2), new TimeOnly(9, 0), 1),
                new Appointment(id2, Guid.NewGuid(), new DateOnly(2031, 1, 2), new TimeOnly(10, 0), 1),
                new Appointment(otherId, Guid.NewGuid(), new DateOnly(2031, 1, 2), new TimeOnly(11, 0), 1));
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetByIdsAsync([id1, id2], CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Id == id1);
        Assert.Contains(result, a => a.Id == id2);
        Assert.DoesNotContain(result, a => a.Id == otherId);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_NewAppointment_PersistsToDatabase()
    {
        var id = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            await MakeRepo(ctx).AddAsync(
                new Appointment(id, Guid.NewGuid(), new DateOnly(2031, 1, 3), new TimeOnly(9, 0), 1),
                CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        await using var verifyCtx = new MedicalCenterDbContext(opts);
        var saved = await verifyCtx.Appointments.FindAsync(id);

        Assert.NotNull(saved);
        Assert.Equal(AppointmentStatus.Libre, saved.Status);
    }

    // ── GetByDateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByDateAsync_AppointmentsOnDate_ReturnsThem()
    {
        var targetDate = new DateOnly(2031, 1, 4);
        var scheduleId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Appointments.AddRange(
                new Appointment(Guid.NewGuid(), scheduleId, targetDate, new TimeOnly(9, 0), 1),
                new Appointment(Guid.NewGuid(), scheduleId, targetDate, new TimeOnly(10, 0), 2),
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 1, 5), new TimeOnly(9, 0), 1));
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetByDateAsync(targetDate, CancellationToken.None);

        Assert.True(result.Count >= 2);
        Assert.All(result, a => Assert.Equal(targetDate, a.Fecha));
    }

    [Fact]
    public async Task GetByDateAsync_NoAppointmentsOnDate_ReturnsEmpty()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var result = await MakeRepo(ctx).GetByDateAsync(new DateOnly(2099, 12, 31), CancellationToken.None);

        Assert.Empty(result);
    }

    // ── GetByRangeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByRangeAsync_AppointmentsInRange_ReturnsThem()
    {
        var start = new DateOnly(2031, 2, 1);
        var end = new DateOnly(2031, 2, 3);
        var scheduleId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Appointments.AddRange(
                new Appointment(Guid.NewGuid(), scheduleId, new DateOnly(2031, 2, 1), new TimeOnly(9, 0), 1),
                new Appointment(Guid.NewGuid(), scheduleId, new DateOnly(2031, 2, 2), new TimeOnly(9, 0), 1),
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 2, 5), new TimeOnly(9, 0), 1));
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetByRangeAsync(start, end, null, null, CancellationToken.None);

        Assert.Contains(result, a => a.ScheduleId == scheduleId);
        Assert.DoesNotContain(result, a => a.Fecha > end);
        Assert.DoesNotContain(result, a => a.Fecha < start);
    }

    [Fact]
    public async Task GetByRangeAsync_WithOffsetAndLimit_RespectsParameters()
    {
        var rangeStart = new DateOnly(2031, 3, 10);
        var rangeEnd = new DateOnly(2031, 3, 14);
        var scheduleId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            for (var i = 0; i < 5; i++)
            {
                ctx.Appointments.Add(new Appointment(
                    Guid.NewGuid(), scheduleId, rangeStart.AddDays(i), new TimeOnly(9, 0), 1));
            }
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetByRangeAsync(rangeStart, rangeEnd, offset: 1, limit: 2, CancellationToken.None);

        Assert.True(result.Count <= 2);
    }

    [Fact]
    public async Task GetByRangeAsync_OutsideRange_ReturnsEmpty()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var result = await MakeRepo(ctx).GetByRangeAsync(
            new DateOnly(2099, 11, 1), new DateOnly(2099, 11, 2), null, null, CancellationToken.None);

        Assert.Empty(result);
    }

    // ── CountByRangeAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CountByRangeAsync_AppointmentsInRange_ReturnsCorrectCount()
    {
        var start = new DateOnly(2031, 4, 1);
        var end = new DateOnly(2031, 4, 3);
        var scheduleId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Appointments.AddRange(
                new Appointment(Guid.NewGuid(), scheduleId, new DateOnly(2031, 4, 1), new TimeOnly(9, 0), 1),
                new Appointment(Guid.NewGuid(), scheduleId, new DateOnly(2031, 4, 2), new TimeOnly(9, 0), 1));
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var count = await MakeRepo(qCtx).CountByRangeAsync(start, end, CancellationToken.None);

        Assert.True(count >= 2);
    }

    [Fact]
    public async Task CountByRangeAsync_NoAppointmentsInRange_ReturnsZero()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var count = await MakeRepo(ctx).CountByRangeAsync(
            new DateOnly(2099, 10, 1), new DateOnly(2099, 10, 2), CancellationToken.None);

        Assert.Equal(0, count);
    }

    // ── GetActivosByPacienteAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetActivosByPacienteAsync_OcupadoAfterFromDate_ReturnsThem()
    {
        var patientId = Guid.NewGuid();
        var fecha = new DateOnly(2031, 5, 1);
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            var appt = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1);
            appt.Reserve(patientId);
            ctx.Appointments.Add(appt);
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetActivosByPacienteAsync(patientId, fecha, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.All(result, a =>
        {
            Assert.Equal(patientId, a.PatientId);
            Assert.Equal(AppointmentStatus.Ocupado, a.Status);
        });
    }

    [Fact]
    public async Task GetActivosByPacienteAsync_AppointmentBeforeFromDate_NotReturned()
    {
        var patientId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            var appt = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 5, 2), new TimeOnly(9, 0), 1);
            appt.Reserve(patientId);
            ctx.Appointments.Add(appt);
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetActivosByPacienteAsync(
            patientId, new DateOnly(2031, 5, 3), CancellationToken.None);

        Assert.Empty(result);
    }

    // ── GetOccupiedByPacienteOnDateAsync ──────────────────────────────────────

    [Fact]
    public async Task GetOccupiedByPacienteOnDateAsync_OcupadoOnDate_ReturnsThem()
    {
        var patientId = Guid.NewGuid();
        var fecha = new DateOnly(2031, 6, 1);
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            var appt = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(10, 0), 1);
            appt.Reserve(patientId);
            ctx.Appointments.Add(appt);
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetOccupiedByPacienteOnDateAsync(patientId, fecha, CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.All(result, a => Assert.Equal(patientId, a.PatientId));
    }

    [Fact]
    public async Task GetOccupiedByPacienteOnDateAsync_DifferentDate_ReturnsEmpty()
    {
        var patientId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            var appt = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 6, 2), new TimeOnly(10, 0), 1);
            appt.Reserve(patientId);
            ctx.Appointments.Add(appt);
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetOccupiedByPacienteOnDateAsync(
            patientId, new DateOnly(2031, 6, 3), CancellationToken.None);

        Assert.Empty(result);
    }

    // ── GetByBlockAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByBlockAsync_MatchingBlock_ReturnsThem()
    {
        var fecha = new DateOnly(2031, 7, 1);
        var hora = new TimeOnly(9, 0);
        const int cameraId = 199;
        var scheduleId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Appointments.AddRange(
                new Appointment(Guid.NewGuid(), scheduleId, fecha, hora, 1, cameraId),
                new Appointment(Guid.NewGuid(), scheduleId, fecha, hora, 2, cameraId),
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(10, 0), 1, cameraId));
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetByBlockAsync(fecha, hora, cameraId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, a =>
        {
            Assert.Equal(fecha, a.Fecha);
            Assert.Equal(hora, a.Hora);
            Assert.Equal(cameraId, a.CameraId);
        });
    }

    [Fact]
    public async Task GetByBlockAsync_NoMatch_ReturnsEmpty()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var result = await MakeRepo(ctx).GetByBlockAsync(
            new DateOnly(2099, 9, 9), new TimeOnly(9, 0), 99999, CancellationToken.None);

        Assert.Empty(result);
    }

    // ── GetByTandaIdAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTandaIdAsync_WithMatchingTandaId_ReturnsThem()
    {
        var tandaId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            var a1 = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 8, 1), new TimeOnly(9, 0), 1);
            a1.AssignTanda(tandaId);
            var a2 = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 8, 1), new TimeOnly(10, 0), 1);
            a2.AssignTanda(tandaId);
            var other = new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 8, 1), new TimeOnly(11, 0), 1);
            ctx.Appointments.AddRange(a1, a2, other);
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetByTandaIdAsync(tandaId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal(tandaId, a.TandaId));
    }

    [Fact]
    public async Task GetByTandaIdAsync_NoMatch_ReturnsEmpty()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var result = await MakeRepo(ctx).GetByTandaIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(result);
    }

    // ── GetFutureExcessByCameraAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetFutureExcessByCameraAsync_MatchingAppointments_ReturnsThem()
    {
        const int cameraId = 188;
        var from = new DateOnly(2031, 9, 1);
        const int minLugar = 3;
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Appointments.AddRange(
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 9, 1), new TimeOnly(9, 0), 3, cameraId),
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 9, 1), new TimeOnly(10, 0), 5, cameraId),
                new Appointment(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2031, 9, 1), new TimeOnly(11, 0), 1, cameraId));
            await ctx.SaveChangesAsync();
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(qCtx).GetFutureExcessByCameraAsync(cameraId, from, minLugar, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, a =>
        {
            Assert.Equal(cameraId, a.CameraId);
            Assert.True(a.Lugar >= minLugar);
            Assert.True(a.Fecha >= from);
        });
    }

    [Fact]
    public async Task GetFutureExcessByCameraAsync_NoMatch_ReturnsEmpty()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var result = await MakeRepo(ctx).GetFutureExcessByCameraAsync(99999, new DateOnly(2099, 1, 1), 1, CancellationToken.None);

        Assert.Empty(result);
    }

    // ── TryReserveAppointmentAsync ────────────────────────────────────────────

    [Fact]
    public async Task TryReserveAppointmentAsync_LibreAppointment_ReservesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Appointments.Add(new Appointment(id, Guid.NewGuid(), new DateOnly(2031, 10, 1), new TimeOnly(9, 0), 1));
            await ctx.SaveChangesAsync();
        }

        bool reserved;
        await using (var commandCtx = new MedicalCenterDbContext(opts))
        {
            reserved = await MakeRepo(commandCtx).TryReserveAppointmentAsync(id, patientId, CancellationToken.None);
        }

        Assert.True(reserved);

        await using var verifyCtx = new MedicalCenterDbContext(opts);
        var saved = await verifyCtx.Appointments.FindAsync(id);
        Assert.Equal(AppointmentStatus.Ocupado, saved!.Status);
        Assert.Equal(patientId, saved.PatientId);
    }

    [Fact]
    public async Task TryReserveAppointmentAsync_OcupadoAppointment_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            var appt = new Appointment(id, Guid.NewGuid(), new DateOnly(2031, 10, 2), new TimeOnly(9, 0), 1);
            appt.Reserve(Guid.NewGuid());
            ctx.Appointments.Add(appt);
            await ctx.SaveChangesAsync();
        }

        await using var commandCtx = new MedicalCenterDbContext(opts);
        var result = await MakeRepo(commandCtx).TryReserveAppointmentAsync(id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task TryReserveAppointmentAsync_UnknownId_ReturnsFalse()
    {
        await using var ctx = new MedicalCenterDbContext(CreateOptions());
        var result = await MakeRepo(ctx).TryReserveAppointmentAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    // ── TryCommitAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task TryCommitAsync_WithStagedChanges_SavesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var opts = CreateOptions();

        await using var ctx = new MedicalCenterDbContext(opts);
        await ctx.Database.EnsureCreatedAsync();
        ctx.Appointments.Add(new Appointment(id, Guid.NewGuid(), new DateOnly(2031, 11, 1), new TimeOnly(9, 0), 1));

        var result = await MakeRepo(ctx).TryCommitAsync(CancellationToken.None);

        Assert.True(result);

        await using var verifyCtx = new MedicalCenterDbContext(opts);
        Assert.NotNull(await verifyCtx.Appointments.FindAsync(id));
    }

    // ── DeleteRangeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRangeAsync_GivenAppointments_RemovesThemFromDatabase()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var keepId = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Appointments.AddRange(
                new Appointment(id1, Guid.NewGuid(), new DateOnly(2031, 11, 2), new TimeOnly(9, 0), 1),
                new Appointment(id2, Guid.NewGuid(), new DateOnly(2031, 11, 2), new TimeOnly(10, 0), 1),
                new Appointment(keepId, Guid.NewGuid(), new DateOnly(2031, 11, 2), new TimeOnly(11, 0), 1));
            await ctx.SaveChangesAsync();
        }

        await using (var commandCtx = new MedicalCenterDbContext(opts))
        {
            var toDelete = await commandCtx.Appointments
                .Where(a => a.Id == id1 || a.Id == id2)
                .ToListAsync();
            var repo = MakeRepo(commandCtx);
            await repo.DeleteRangeAsync(toDelete, CancellationToken.None);
            await commandCtx.SaveChangesAsync();
        }

        await using var verifyCtx = new MedicalCenterDbContext(opts);
        Assert.Null(await verifyCtx.Appointments.FindAsync(id1));
        Assert.Null(await verifyCtx.Appointments.FindAsync(id2));
        Assert.NotNull(await verifyCtx.Appointments.FindAsync(keepId));
    }

    // ── TryCommitWithPatientLockAsync ─────────────────────────────────────────

    [Fact]
    public async Task TryCommitWithPatientLockAsync_NoConsecutiveConflict_CommitsAndReturnsTrue()
    {
        var patientId = Guid.NewGuid();
        var fecha = new DateOnly(2031, 12, 1);
        var id = Guid.NewGuid();
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        await using var commandCtx = new MedicalCenterDbContext(opts);
        var appt = new Appointment(id, Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1);
        appt.Reserve(patientId);
        commandCtx.Appointments.Add(appt);

        var result = await MakeRepo(commandCtx)
            .TryCommitWithPatientLockAsync(patientId, fecha, new TimeOnly(9, 0), null, CancellationToken.None);

        Assert.True(result);

        await using var verifyCtx = new MedicalCenterDbContext(opts);
        Assert.NotNull(await verifyCtx.Appointments.FindAsync(id));
    }

    [Fact]
    public async Task TryCommitWithPatientLockAsync_ConsecutiveAppointmentWithin60Min_ThrowsConflictException()
    {
        var patientId = Guid.NewGuid();
        var fecha = new DateOnly(2031, 12, 2);
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.EnsureCreatedAsync();
            var existing = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 0), 1);
            existing.Reserve(patientId);
            ctx.Appointments.Add(existing);
            await ctx.SaveChangesAsync();
        }

        await using var commandCtx = new MedicalCenterDbContext(opts);
        var newAppt = new Appointment(Guid.NewGuid(), Guid.NewGuid(), fecha, new TimeOnly(9, 30), 2);
        newAppt.Reserve(patientId);
        commandCtx.Appointments.Add(newAppt);

        await Assert.ThrowsAsync<ConflictException>(() =>
            MakeRepo(commandCtx).TryCommitWithPatientLockAsync(
                patientId, fecha, new TimeOnly(9, 30), null, CancellationToken.None));
    }
}
