using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class MedicoRepository(MedicalCenterDbContext dbContext) : IMedicoRepository
{
    public Task<Medico?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Medicos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Medico>> GetAsync(bool onlyActive, CancellationToken cancellationToken)
    {
        var query = dbContext.Medicos.AsQueryable();
        if (onlyActive)
        {
            query = query.Where(x => x.Activo);
        }

        return await query.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
    }

    public Task<Medico?> GetByNormalizedNameAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken)
    {
        var query = dbContext.Medicos.AsQueryable();
        if (exceptId.HasValue)
        {
            query = query.Where(x => x.Id != exceptId.Value);
        }

        return query.FirstOrDefaultAsync(x => x.Nombre.ToLower() == normalizedName.ToLower(), cancellationToken);
    }

    public async Task<int> GetNextOrderAsync(CancellationToken cancellationToken)
    {
        var max = await dbContext.Medicos.Select(x => (int?)x.Orden).MaxAsync(cancellationToken);
        return (max ?? 0) + 1;
    }

    public Task AddAsync(Medico medico, CancellationToken cancellationToken) =>
        dbContext.Medicos.AddAsync(medico, cancellationToken).AsTask();
}
