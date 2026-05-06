using System.Data;
using System.Data.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private const string CacheKey = "mc:rbac:roles";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);

    private readonly MedicalCenterDbContext _dbContext;
    private readonly ICacheService _cache;

    public RoleRepository(MedicalCenterDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<Role>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                r.slug,
                r.nombre,
                r.descripcion,
                r.activo,
                r.is_system,
                r.is_staff,
                r.default_home,
                coalesce(
                    array_agg(p.key order by p.key) filter (where p.key is not null),
                    array[]::text[]
                ) as permissions
            from public.perfiles p0
            join public.rbac_user_roles ur on ur.user_id = p0.id and ur.expires_at is null
            join public.rbac_roles r on r.id = ur.role_id and r.activo = true
            left join public.rbac_role_permissions rp on rp.role_id = r.id and rp.granted = true
            left join public.rbac_permissions p on p.id = rp.permission_id
            where p0.auth_user_id = @userId or p0.id = @userId
            group by r.slug, r.nombre, r.descripcion, r.activo, r.is_system, r.is_staff, r.default_home
            order by r.is_system desc, r.slug asc;
            """;

        var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new NpgsqlParameter<Guid>("userId", userId));

            var roles = new List<Role>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var permissions = await ReadPermissionsAsync(reader, cancellationToken);
                var description = await ReadDescriptionAsync(reader, cancellationToken);
                var role = new Role(new RoleCreateParams(
                    Guid.NewGuid(),
                    reader.GetString(0),
                    reader.GetString(1),
                    permissions,
                    description,
                    reader.GetBoolean(3),
                    reader.GetBoolean(4),
                    reader.GetBoolean(5),
                    reader.GetString(6)));
                roles.Add(role);
            }

            return roles;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<string[]> ReadPermissionsAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        if (await reader.IsDBNullAsync(7, cancellationToken))
        {
            return [];
        }

        return await reader.GetFieldValueAsync<string[]>(7, cancellationToken);
    }

    private static async Task<string?> ReadDescriptionAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        if (await reader.IsDBNullAsync(2, cancellationToken))
        {
            return null;
        }

        return await reader.GetFieldValueAsync<string>(2, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetOrSetAsync(
            CacheKey,
            async () => await _dbContext.Roles.OrderBy(x => x.Code).ToListAsync(cancellationToken),
            CacheTtl,
            cancellationToken);
    }

    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _dbContext.Roles.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken)
    {
        await _dbContext.Roles.AddAsync(role, cancellationToken);
        await _cache.RemoveAsync(CacheKey, cancellationToken);
    }
}
