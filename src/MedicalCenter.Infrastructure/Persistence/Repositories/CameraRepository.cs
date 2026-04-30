using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class CameraRepository(MedicalCenterDbContext dbContext) : ICameraRepository
{
    public async Task<IReadOnlyCollection<Camera>> GetAsync(CancellationToken cancellationToken) =>
        await dbContext.Cameras.OrderBy(x => x.Id).ToListAsync(cancellationToken);

    public Task<Camera?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Cameras.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken) =>
        (await dbContext.Cameras.MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0) + 1;

    public Task AddAsync(Camera camera, CancellationToken cancellationToken) =>
        dbContext.Cameras.AddAsync(camera, cancellationToken).AsTask();
}
