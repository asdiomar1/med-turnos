using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class ObraSocialRepository : IObraSocialRepository
{
    private const string CacheKey = "mc:catalog:obrasocial";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1440);

    private readonly MedicalCenterDbContext _dbContext;
    private readonly ICacheService _cache;

    public ObraSocialRepository(MedicalCenterDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<ObraSocial>> GetAllAsync(CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync(
            CacheKey,
            async () => await _dbContext.ObrasSociales.AsNoTracking().OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync(cancellationToken),
            CacheTtl,
            cancellationToken);

    public Task<ObraSocial?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _dbContext.ObrasSociales.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<ObraSocial?> GetByNormalizedNameAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken)
    {
        var query = _dbContext.ObrasSociales.AsQueryable();
        if (exceptId.HasValue)
        {
            query = query.Where(x => x.Id != exceptId.Value);
        }

        return query.FirstOrDefaultAsync(x => x.Nombre.Equals(normalizedName, StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    public async Task AddAsync(ObraSocial obraSocial, CancellationToken cancellationToken)
    {
        await _dbContext.ObrasSociales.AddAsync(obraSocial, cancellationToken);
        await _cache.RemoveAsync(CacheKey, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ObraSocial>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return [];
        }

        // Cache-independent — always queries DB directly to avoid stale batch data
        return await _dbContext.ObrasSociales
            .AsNoTracking()
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public Task InvalidateCacheAsync(CancellationToken cancellationToken) =>
        _cache.RemoveAsync(CacheKey, cancellationToken);
}
