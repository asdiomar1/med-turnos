using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class ReferenteRepository(MedicalCenterDbContext dbContext) : IReferenteRepository
{
    public Task<Referente?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Referentes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Referente>> GetAsync(CancellationToken cancellationToken) =>
        await dbContext.Referentes.AsNoTracking().OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync(cancellationToken);

    public Task<Referente?> GetByNormalizedNameAndTypeAsync(string normalizedName, string normalizedType, int? exceptId, CancellationToken cancellationToken)
    {
        var query = dbContext.Referentes.AsQueryable();
        if (exceptId.HasValue)
        {
            query = query.Where(x => x.Id != exceptId.Value);
        }

        return query.FirstOrDefaultAsync(x =>
            string.Equals(x.Nombre.Trim(), normalizedName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Tipo, normalizedType, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task<int> GetNextOrderAsync(CancellationToken cancellationToken)
    {
        var max = await dbContext.Referentes.Select(x => (int?)x.Orden).MaxAsync(cancellationToken);
        return (max ?? 0) + 1;
    }

    public Task AddAsync(Referente referente, CancellationToken cancellationToken) =>
        dbContext.Referentes.AddAsync(referente, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<Referente>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return [];
        }

        return await dbContext.Referentes
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }
}
