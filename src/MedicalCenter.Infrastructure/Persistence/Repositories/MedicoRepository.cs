using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class MedicoRepository(MedicalCenterDbContext dbContext) : IMedicoRepository
{
    public Task<Medico?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Medicos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Medico?> GetByMedicoUserIdAsync(Guid? medicoUserId, CancellationToken cancellationToken)
    {
        if (!medicoUserId.HasValue || medicoUserId.Value == Guid.Empty)
        {
            return Task.FromResult<Medico?>(null);
        }

        return dbContext.Medicos.FirstOrDefaultAsync(x => x.PerfilId == medicoUserId.Value, cancellationToken);
    }

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

        return dbContext.Medicos.FirstOrDefaultAsync(x => x.Nombre.Equals(normalizedName, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task<int> GetNextOrderAsync(CancellationToken cancellationToken)
    {
        var max = await dbContext.Medicos.Select(x => (int?)x.Orden).MaxAsync(cancellationToken);
        return (max ?? 0) + 1;
    }

    public Task AddAsync(Medico medico, CancellationToken cancellationToken) =>
        dbContext.Medicos.AddAsync(medico, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<Medico>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return [];
        }

        return await dbContext.Medicos
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Medico>> GetByMedicoUserIdsAsync(IEnumerable<Guid> medicoUserIds, CancellationToken cancellationToken)
    {
        var distinctIds = medicoUserIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return [];
        }

        return await dbContext.Medicos
            .Where(x => x.PerfilId != null && distinctIds.Contains(x.PerfilId.Value))
            .ToListAsync(cancellationToken);
    }
}
