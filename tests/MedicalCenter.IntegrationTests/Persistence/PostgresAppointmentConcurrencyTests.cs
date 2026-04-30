using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace MedicalCenter.IntegrationTests.Persistence;

public sealed class PostgresAppointmentConcurrencyTests
{
    private readonly string _databaseName = $"medical_center_it_{Guid.NewGuid():N}";
    private readonly string _adminConnectionString = Environment.GetEnvironmentVariable("MEDICALCENTER_TEST_ADMIN_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;SSL Mode=Disable;Channel Binding=Disable;Timeout=5";

    private string DatabaseConnectionString =>
        Environment.GetEnvironmentVariable("MEDICALCENTER_TEST_CONNECTION")
        ?? $"Host=localhost;Port=5432;Database={_databaseName};Username=postgres;Password=postgres;SSL Mode=Disable;Channel Binding=Disable;Timeout=5";

    [Fact]
    public async Task ConcurrentReserve_OnlyOneCommitSucceeds()
    {
        try
        {
            await CreateDatabaseAsync();
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            return;
        }

        try
        {
            var options = CreateOptions();
            var appointmentId = Guid.NewGuid();
            var scheduleId = Guid.NewGuid();
            var patient1 = Guid.NewGuid();
            var patient2 = Guid.NewGuid();

            await using (var setupContext = new MedicalCenterDbContext(options))
            {
                await setupContext.Database.MigrateAsync();
                setupContext.Appointments.Add(new Appointment(appointmentId, scheduleId, new DateOnly(2026, 4, 25), new TimeOnly(10, 0), 1, 1));
                await setupContext.SaveChangesAsync();
            }

            await using var context1 = new MedicalCenterDbContext(options);
            await using var context2 = new MedicalCenterDbContext(options);
            var repository1 = new AppointmentRepository(context1, NullLogger<AppointmentRepository>.Instance);
            var repository2 = new AppointmentRepository(context2, NullLogger<AppointmentRepository>.Instance);

            var slot1 = await repository1.GetByIdAsync(appointmentId, CancellationToken.None);
            var slot2 = await repository2.GetByIdAsync(appointmentId, CancellationToken.None);

            Assert.NotNull(slot1);
            Assert.NotNull(slot2);

            slot1!.Reserve(patient1);
            slot2!.Reserve(patient2);

            var results = await Task.WhenAll(
                repository1.TryCommitAsync(CancellationToken.None),
                repository2.TryCommitAsync(CancellationToken.None));

            Assert.Single(results, x => x);
            Assert.Single(results, x => !x);

            await using var verifyContext = new MedicalCenterDbContext(options);
            var persisted = await verifyContext.Appointments.SingleAsync(x => x.Id == appointmentId);
            Assert.Equal(MedicalCenter.Domain.Enums.AppointmentStatus.Ocupado, persisted.Status);
            Assert.True(persisted.PatientId is not null && (persisted.PatientId == patient1 || persisted.PatientId == patient2));
        }
        finally
        {
            await DropDatabaseAsync();
        }
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", connection);
        await command.ExecuteNonQueryAsync();
    }

    private DbContextOptions<MedicalCenterDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(DatabaseConnectionString)
            .Options;

    private async Task DropDatabaseAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                             $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid()",
                             connection))
            {
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\"", connection);
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup for optional real-Postgres test.
        }
    }
}
