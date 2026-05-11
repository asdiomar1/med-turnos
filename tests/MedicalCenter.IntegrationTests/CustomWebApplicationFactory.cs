using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace MedicalCenter.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("medical_center_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Apply migrations using direct connection (before webhost configures services)
        var optionsBuilder = new DbContextOptionsBuilder<MedicalCenterDbContext>();
        optionsBuilder.UseNpgsql(_postgres.GetConnectionString());
        
        await using var dbContext = new MedicalCenterDbContext(optionsBuilder.Options);
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
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
                options.UseNpgsql(_postgres.GetConnectionString());
            });
        });
    }
}
