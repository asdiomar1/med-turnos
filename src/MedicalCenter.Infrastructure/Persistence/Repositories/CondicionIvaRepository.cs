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
}
