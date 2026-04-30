using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MedicalCenter.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MedicalCenterDbContext>
{
    public MedicalCenterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MedicalCenterDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? "Host=localhost;Port=5432;Database=medical_center;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);
        return new MedicalCenterDbContext(optionsBuilder.Options);
    }
}
