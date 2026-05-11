using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class CondicionIvaRepository : ICondicionIvaRepository
{
    private const string CacheKey = "mc:catalog:condicioniva";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1440);

    private readonly MedicalCenterDbContext _dbContext;
    private readonly ICacheService _cache;

    public CondicionIvaRepository(MedicalCenterDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<CondicionIva>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var key = includeInactive ? $"{CacheKey}:all" : CacheKey;
        return await _cache.GetOrSetAsync(
            key,
            async () =>
            {
                var query = _dbContext.CondicionesIva.AsNoTracking().AsQueryable();
                if (!includeInactive)
                {
                    query = query.Where(x => x.Activo);
                }

                return await query.OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync(cancellationToken);
            },
            CacheTtl,
            cancellationToken);
    }

    public Task<CondicionIva?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _dbContext.CondicionesIva.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CondicionIva?> GetByNormalizedNameAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken)
    {
        var query = _dbContext.CondicionesIva.AsQueryable();
        if (exceptId.HasValue)
        {
            query = query.Where(x => x.Id != exceptId.Value);
        }

        return query.FirstOrDefaultAsync(x => x.Nombre.Equals(normalizedName, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task<int> GetNextOrderAsync(CancellationToken cancellationToken)
    {
        var max = await _dbContext.CondicionesIva.Select(x => (int?)x.Orden).MaxAsync(cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task AddAsync(CondicionIva condicionIva, CancellationToken cancellationToken)
    {
        await _dbContext.CondicionesIva.AddAsync(condicionIva, cancellationToken);
        await InvalidateCacheAsync(cancellationToken);
    }

    public async Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(CacheKey, cancellationToken);
        await _cache.RemoveAsync($"{CacheKey}:all", cancellationToken);
    }
}
