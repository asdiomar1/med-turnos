using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MedicalCenter.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"medical_center_it_{Guid.NewGuid():N}";
    private readonly string _adminConnectionString =
        Environment.GetEnvironmentVariable("INTEGRATION_TEST_ADMIN_DB_CONNECTION")
        ?? "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=postgres";
    private readonly string _connectionString;

    public CustomWebApplicationFactory()
    {
        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _databaseName
        };

        _connectionString = builder.ConnectionString;
    }

    public string ConnectionString => _connectionString;

    public async Task InitializeAsync()
    {
        await EnsureDatabaseCreatedAsync();

        // Wait for database to be ready with retry
        Exception? lastException = null;
        var maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<MedicalCenterDbContext>();
                optionsBuilder.UseNpgsql(_connectionString);
                
                await using var dbContext = new MedicalCenterDbContext(optionsBuilder.Options);

                await dbContext.Database.MigrateAsync();
                return; // Success
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (i < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }
        
        // All retries failed - throw to see the actual error
        throw new InvalidOperationException(
            $"Failed to apply migrations after {maxRetries} attempts. See inner exception for details.", 
            lastException);
    }

    public new async Task DisposeAsync()
    {
        await DropDatabaseAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Jwt:SecretKey"] = "this-is-a-32-char-long-secret-key!",
                ["SkipDatabaseInitialization"] = "false"
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MedicalCenterDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<MedicalCenterDbContext>(options =>
            {
                options.UseNpgsql(_connectionString);
            });
        });
    }

    private async Task EnsureDatabaseCreatedAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        var createSql = $"CREATE DATABASE \"{_databaseName}\"";
        await using var command = new NpgsqlCommand(createSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();

            var terminateSql = $@"
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();";

            await using (var terminate = new NpgsqlCommand(terminateSql, connection))
            {
                await terminate.ExecuteNonQueryAsync();
            }

            var dropSql = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
            await using var drop = new NpgsqlCommand(dropSql, connection);
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}
