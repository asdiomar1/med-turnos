using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Infrastructure.Persistence;

namespace MedicalCenter.Infrastructure.Configuration;

public sealed class EfUnitOfWork(MedicalCenterDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
