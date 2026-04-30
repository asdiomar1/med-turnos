using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class CampoConfigRepository : ICampoConfigRepository
{
    private const string CacheKey = "mc:catalog:campoconfig";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1440);

    private readonly MedicalCenterDbContext _dbContext;
    private readonly ICacheService _cache;

    public CampoConfigRepository(MedicalCenterDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<CampoConfig>> GetAllAsync(CancellationToken cancellationToken) =>
        await _cache.GetOrSetAsync(
            CacheKey,
            async () => await _dbContext.CamposConfig.AsNoTracking().OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync(cancellationToken),
            CacheTtl,
            cancellationToken);

    public Task<CampoConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.CamposConfig.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CampoConfig?> GetByNormalizedNameAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken)
    {
        var query = _dbContext.CamposConfig.AsQueryable();
        if (exceptId.HasValue)
        {
            query = query.Where(x => x.Id != exceptId.Value);
        }

        return query.FirstOrDefaultAsync(x => x.Nombre.ToLower().Trim() == normalizedName.ToLower().Trim(), cancellationToken);
    }

    public async Task<int> GetNextOrderAsync(CancellationToken cancellationToken)
    {
        var max = await _dbContext.CamposConfig.Select(x => (int?)x.Orden).MaxAsync(cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task AddAsync(CampoConfig campoConfig, CancellationToken cancellationToken)
    {
        await _dbContext.CamposConfig.AddAsync(campoConfig, cancellationToken);
        await _cache.RemoveAsync(CacheKey, cancellationToken);
    }

    public void Remove(CampoConfig campoConfig)
    {
        _dbContext.CamposConfig.Remove(campoConfig);
        _ = _cache.RemoveAsync(CacheKey, CancellationToken.None);
    }
}
