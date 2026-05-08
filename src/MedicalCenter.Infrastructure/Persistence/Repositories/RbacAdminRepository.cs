using System.Data;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class RbacAdminRepository : IRbacAdminRepository
{
    private const string PermissionsCacheKey = "mc:rbac:permissions";
    private const string RolesCacheKey = "mc:rbac:roles:admin";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(60);

    private readonly MedicalCenterDbContext _dbContext;
    private readonly ICacheService _cache;

    public RbacAdminRepository(MedicalCenterDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<RbacPermissionSummary>> ListPermissionsAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetOrSetAsync(
            PermissionsCacheKey,
            async () =>
            {
                const string sql = """
                    select key, nombre, descripcion, modulo, is_system
                    from public.rbac_permissions
                    order by key asc;
                    """;

                return await QueryAsync(sql, reader => new RbacPermissionSummary(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetString(3),
                    reader.GetBoolean(4)), cancellationToken);
            },
            CacheTtl,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<RbacRoleSummary>> ListRolesAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetOrSetAsync(
            RolesCacheKey,
            async () =>
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
                    from public.rbac_roles r
                    left join public.rbac_role_permissions rp
                        on rp.role_id = r.id
                       and rp.granted = true
                    left join public.rbac_permissions p
                        on p.id = rp.permission_id
                    group by r.slug, r.nombre, r.descripcion, r.activo, r.is_system, r.is_staff, r.default_home
                    order by r.is_system desc, r.slug asc;
                    """;

                return await QueryAsync(sql, reader => new RbacRoleSummary(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetBoolean(3),
                    reader.GetBoolean(4),
                    reader.GetBoolean(5),
                    reader.GetString(6),
                    reader.IsDBNull(7) ? [] : reader.GetFieldValue<string[]>(7)), cancellationToken);
            },
            CacheTtl,
            cancellationToken);
    }

    public async Task<RbacRoleSummary?> GetRoleBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return (await ListRolesAsync(cancellationToken)).FirstOrDefault(x => string.Equals(x.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<RbacRoleSummary> UpsertRoleAsync(UpsertRbacRoleCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Slug) || string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new ValidationException("slug y nombre son obligatorios");
        }

        var slug = command.Slug.Trim().ToLowerInvariant();
        var nombre = command.Nombre.Trim();
        var descripcion = string.IsNullOrWhiteSpace(command.Descripcion) ? null : command.Descripcion.Trim();
        var defaultHome = string.IsNullOrWhiteSpace(command.DefaultHome) ? "/usuario" : command.DefaultHome.Trim();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var roleId = await ExecuteScalarAsync<long>(
                """
                insert into public.rbac_roles (slug, nombre, descripcion, activo, is_system, is_staff, default_home)
                values (@slug, @nombre, @descripcion, @activo, @is_system, @is_staff, @default_home)
                on conflict (slug) do update
                set
                    nombre = excluded.nombre,
                    descripcion = excluded.descripcion,
                    activo = excluded.activo,
                    is_system = excluded.is_system,
                    is_staff = excluded.is_staff,
                    default_home = excluded.default_home,
                    updated_at = now()
                returning id;
                """,
                cancellationToken,
                transaction,
                new NpgsqlParameter<string>("slug", slug),
                new NpgsqlParameter<string>("nombre", nombre),
                new NpgsqlParameter("descripcion", (object?)descripcion ?? DBNull.Value),
                new NpgsqlParameter<bool>("activo", command.Activo),
                new NpgsqlParameter<bool>("is_system", command.IsSystem),
                new NpgsqlParameter<bool>("is_staff", command.IsStaff),
                new NpgsqlParameter<string>("default_home", defaultHome));

            await ReplaceRolePermissionsAsync(roleId, command.Permissions, cancellationToken, transaction);
            await RebuildEffectivePermissionsForRoleAsync(roleId, cancellationToken, transaction);
            await transaction.CommitAsync(cancellationToken);

            await _cache.RemoveAsync(RolesCacheKey, cancellationToken);
            await _cache.RemoveAsync(PermissionsCacheKey, cancellationToken);

            return await GetRoleByIdAsync(roleId, cancellationToken) ?? throw new NotFoundException("Rol no encontrado");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SetRolePermissionsAsync(string roleSlug, IReadOnlyCollection<string> permissionKeys, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleSlug))
        {
            throw new ValidationException("role_slug es obligatorio");
        }

        var role = await GetRoleBySlugAsync(roleSlug, cancellationToken) ?? throw new NotFoundException("Rol no encontrado");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var roleId = await GetRoleIdAsync(role.Slug, cancellationToken, transaction) ?? throw new NotFoundException("Rol no encontrado");
            await ReplaceRolePermissionsAsync(roleId, permissionKeys, cancellationToken, transaction);
            await RebuildEffectivePermissionsForRoleAsync(roleId, cancellationToken, transaction);
            await transaction.CommitAsync(cancellationToken);

            await _cache.RemoveAsync(RolesCacheKey, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<RbacStaffUserSummary>> ListStaffUsersAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                p.id,
                p.nombre,
                coalesce(p.email, '') as email,
                p.auth_user_id,
                p.rol as legacy_rol,
                exists (
                    select 1
                    from public.rbac_user_roles ur_active
                    join public.rbac_roles r_active on r_active.id = ur_active.role_id
                    where ur_active.user_id = p.id
                      and ur_active.expires_at is null
                      and r_active.slug <> 'staff_inactivo'
                ) as is_active,
                coalesce(
                    array_agg(distinct r.slug order by r.slug) filter (where r.slug is not null),
                    array[]::text[]
                ) as roles,
                max(case when ur.is_primary then r.slug else null end) as primary_role
            from public.perfiles p
            join public.rbac_user_roles ur on ur.user_id = p.id and ur.expires_at is null
            join public.rbac_roles r on r.id = ur.role_id and r.is_staff = true
            group by p.id, p.nombre, p.email, p.auth_user_id, p.rol
            order by p.nombre asc nulls last, p.email asc nulls last;
            """;

        var rows = await QueryAsync(sql, reader => new RbacStaffUserSummary(
            reader.GetGuid(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetBoolean(5),
            reader.IsDBNull(6) ? [] : reader.GetFieldValue<string[]>(6),
            reader.IsDBNull(7) ? null : reader.GetString(7)), cancellationToken);

        return includeInactive ? rows : rows.Where(x => x.IsActive).ToArray();
    }

    public async Task<RbacStaffUserSummary> CreateStaffUserAsync(
        string nombre,
        string? email,
        string identifier,
        string passwordHash,
        string roleSlug,
        bool primary,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(roleSlug))
        {
            throw new ValidationException("Datos incompletos");
        }

        var authUserId = Guid.NewGuid();
        var normalizedNombre = nombre.Trim();
        var normalizedIdentifier = identifier.Trim().ToLowerInvariant();
        var normalizedRoleSlug = roleSlug.Trim().ToLowerInvariant();
        var normalizedEmail = string.IsNullOrWhiteSpace(email)
            ? $"staff+{authUserId:N}@internal.local"
            : email.Trim().ToLowerInvariant();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var profileId = Guid.NewGuid();
            var user = new User(new UserCreateParams(authUserId, normalizedIdentifier, normalizedEmail, passwordHash, true, true, null, normalizedNombre));
            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await UpsertProfileAsync(profileId, authUserId, normalizedNombre, normalizedEmail, normalizedIdentifier, cancellationToken, transaction);
            await ReplaceUserRolesAsync(profileId, [normalizedRoleSlug], normalizedRoleSlug, cancellationToken, transaction);
            await RebuildEffectivePermissionsForUserAsync(profileId, cancellationToken, transaction);
            await transaction.CommitAsync(cancellationToken);

            await _cache.RemoveAsync(RolesCacheKey, cancellationToken);

            return await GetStaffUserByAuthUserIdAsync(authUserId, cancellationToken)
                ?? new RbacStaffUserSummary(profileId, normalizedNombre, normalizedEmail, authUserId, true, [normalizedRoleSlug], normalizedRoleSlug);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AssignUserRolesAsync(AssignRbacUserRolesCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ValidationException("user_id es obligatorio");
        }

        var roleSlugs = command.RoleSlugs
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roleSlugs.Length == 0)
        {
            throw new ValidationException("Debe seleccionar al menos un rol");
        }

        var primaryRoleSlug = string.IsNullOrWhiteSpace(command.PrimaryRoleSlug)
            ? roleSlugs[0]
            : command.PrimaryRoleSlug.Trim().ToLowerInvariant();
        if (!roleSlugs.Contains(primaryRoleSlug, StringComparer.OrdinalIgnoreCase))
        {
            primaryRoleSlug = roleSlugs[0];
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var profileId = await GetProfileIdByAuthUserIdOrProfileIdAsync(command.UserId, cancellationToken, transaction)
                ?? throw new NotFoundException("Usuario no encontrado");

            await ReplaceUserRolesAsync(profileId, roleSlugs, primaryRoleSlug, cancellationToken, transaction);
            await RebuildEffectivePermissionsForUserAsync(profileId, cancellationToken, transaction);
            await transaction.CommitAsync(cancellationToken);

            await _cache.RemoveAsync(RolesCacheKey, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SetStaffUserActiveAsync(SetStaffUserActiveCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ValidationException("user_id es obligatorio");
        }

        var targetRoleSlug = string.IsNullOrWhiteSpace(command.RoleSlug)
            ? "secretaria"
            : command.RoleSlug.Trim().ToLowerInvariant();

        if (targetRoleSlug is "paciente" or "staff_inactivo" or "")
        {
            targetRoleSlug = "secretaria";
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var profileId = await GetProfileIdByAuthUserIdOrProfileIdAsync(command.UserId, cancellationToken, transaction)
                ?? throw new NotFoundException("Usuario no encontrado");
            var authUserId = await GetAuthUserIdByProfileIdAsync(profileId, cancellationToken, transaction) ?? command.UserId;

            if (!command.Active)
            {
                await ReplaceUserRolesAsync(profileId, ["staff_inactivo"], "staff_inactivo", cancellationToken, transaction);
            }
            else
            {
                await ReplaceUserRolesAsync(profileId, [targetRoleSlug], targetRoleSlug, cancellationToken, transaction);
            }

            await RebuildEffectivePermissionsForUserAsync(profileId, cancellationToken, transaction);
            await ExecuteAsync(
                "update public.users set \"IsActive\" = @active where \"Id\" = @id;",
                cancellationToken,
                transaction,
                new NpgsqlParameter("active", command.Active),
                new NpgsqlParameter("id", authUserId));
            await transaction.CommitAsync(cancellationToken);

            await _cache.RemoveAsync(RolesCacheKey, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<StaffProfileSummary> UpdateMyDataAsync(Guid authUserId, string nombre, CancellationToken cancellationToken)
    {
        if (authUserId == Guid.Empty)
        {
            throw new ValidationException("user_id es obligatorio");
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ValidationException("nombre es obligatorio");
        }

        var normalizedName = nombre.Trim();
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var profileId = await GetProfileIdByAuthUserIdOrProfileIdAsync(authUserId, cancellationToken, transaction)
                ?? throw new NotFoundException("Usuario no encontrado");

            await ExecuteAsync(
                "update public.perfiles set nombre = @nombre, updated_at = now() where id = @id;",
                cancellationToken,
                transaction,
                new NpgsqlParameter<string>("nombre", normalizedName),
                new NpgsqlParameter<Guid>("id", profileId));

            await ExecuteAsync(
                "update public.users set \"Nombre\" = @nombre where \"Id\" = @id;",
                cancellationToken,
                transaction,
                new NpgsqlParameter<string>("nombre", normalizedName),
                new NpgsqlParameter<Guid>("id", authUserId));

            await transaction.CommitAsync(cancellationToken);
            return await GetStaffProfileByAuthUserIdAsync(authUserId, cancellationToken)
                ?? new StaffProfileSummary(authUserId, authUserId.ToString("N"), null, normalizedName, true, true, []);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<RbacRoleSummary?> GetRoleByIdAsync(long roleId, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
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
            from public.rbac_roles r
            left join public.rbac_role_permissions rp
                on rp.role_id = r.id
               and rp.granted = true
            left join public.rbac_permissions p
                on p.id = rp.permission_id
            where r.id = @roleId
            group by r.slug, r.nombre, r.descripcion, r.activo, r.is_system, r.is_staff, r.default_home;
            """;

        var rows = await QueryAsync(sql, reader => new RbacRoleSummary(
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetBoolean(3),
            reader.GetBoolean(4),
            reader.GetBoolean(5),
            reader.GetString(6),
            reader.IsDBNull(7) ? [] : reader.GetFieldValue<string[]>(7)), cancellationToken, transaction, new NpgsqlParameter<long>("roleId", roleId));

        return rows.SingleOrDefault();
    }

    private async Task<RbacStaffUserSummary?> GetStaffUserByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        const string sql = """
            select
                p.id,
                p.nombre,
                coalesce(p.email, '') as email,
                p.auth_user_id,
                p.rol as legacy_rol,
                exists (
                    select 1
                    from public.rbac_user_roles ur_active
                    join public.rbac_roles r_active on r_active.id = ur_active.role_id
                    where ur_active.user_id = p.id
                      and ur_active.expires_at is null
                      and r_active.slug <> 'staff_inactivo'
                ) as is_active,
                coalesce(
                    array_agg(distinct r.slug order by r.slug) filter (where r.slug is not null),
                    array[]::text[]
                ) as roles,
                max(case when ur.is_primary then r.slug else null end) as primary_role
            from public.perfiles p
            join public.rbac_user_roles ur on ur.user_id = p.id and ur.expires_at is null
            join public.rbac_roles r on r.id = ur.role_id and r.is_staff = true
            where p.auth_user_id = @authUserId
            group by p.id, p.nombre, p.email, p.auth_user_id, p.rol
            limit 1;
            """;

        var rows = await QueryAsync(sql, reader => new RbacStaffUserSummary(
            reader.GetGuid(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetBoolean(5),
            reader.IsDBNull(6) ? [] : reader.GetFieldValue<string[]>(6),
            reader.IsDBNull(7) ? null : reader.GetString(7)), cancellationToken, transaction, new NpgsqlParameter<Guid>("authUserId", authUserId));

        return rows.SingleOrDefault();
    }

    private async Task<StaffProfileSummary?> GetStaffProfileByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        var staff = await GetStaffUserByAuthUserIdAsync(authUserId, cancellationToken, transaction);
        if (staff is null)
        {
            return null;
        }

        return new StaffProfileSummary(
            staff.AuthUserId ?? staff.Id,
            staff.AuthUserId?.ToString("N") ?? staff.Id.ToString("N"),
            staff.Email,
            staff.Nombre,
            staff.IsActive,
            true,
            staff.Roles);
    }

    private async Task<long?> GetRoleIdAsync(string slug, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        const string sql = """
            select id
            from public.rbac_roles
            where slug = @slug
            limit 1;
            """;

        var rows = await QueryAsync(sql, reader => reader.GetInt64(0), cancellationToken, transaction, new NpgsqlParameter<string>("slug", slug));
        return rows.SingleOrDefault();
    }

    private async Task<Guid?> GetProfileIdByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        const string sql = """
            select id
            from public.perfiles
            where auth_user_id = @authUserId
            limit 1;
            """;

        var rows = await QueryAsync(sql, reader => (Guid?)reader.GetGuid(0), cancellationToken, transaction, new NpgsqlParameter("authUserId", authUserId));
        return rows.SingleOrDefault();
    }

    private async Task<Guid?> GetAuthUserIdByProfileIdAsync(Guid profileId, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        const string sql = """
            select auth_user_id
            from public.perfiles
            where id = @profileId
            limit 1;
            """;

        var rows = await QueryAsync<Guid?>(sql, reader => reader.IsDBNull(0) ? null : reader.GetGuid(0), cancellationToken, transaction, new NpgsqlParameter("profileId", profileId));
        return rows.SingleOrDefault();
    }

    private async Task<Guid?> GetProfileIdByAuthUserIdOrProfileIdAsync(Guid userId, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        var authProfileId = await GetProfileIdByAuthUserIdAsync(userId, cancellationToken, transaction);
        if (authProfileId.HasValue)
        {
            return authProfileId;
        }

        const string sql = """
            select id
            from public.perfiles
            where id = @userId
            limit 1;
            """;

        var rows = await QueryAsync(sql, reader => (Guid?)reader.GetGuid(0), cancellationToken, transaction, new NpgsqlParameter("userId", userId));
        return rows.SingleOrDefault();
    }

    private async Task UpsertProfileAsync(Guid profileId, Guid authUserId, string nombre, string email, string loginIdentifier, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        await ExecuteAsync(
            """
            insert into public.perfiles (
                id,
                nombre,
                email,
                telefono,
                documento_identidad,
                nacionalidad,
                rol,
                auth_user_id,
                portal_habilitado,
                requiere_reset_portal,
                portal_login_email,
                documento_identidad_normalizado
            )
            values (
                @id,
                @nombre,
                @email,
                null,
                null,
                null,
                'admin',
                @authUserId,
                false,
                false,
                @portalLoginEmail,
                null
            )
            on conflict (id) do update
            set
                nombre = excluded.nombre,
                email = excluded.email,
                auth_user_id = excluded.auth_user_id,
                portal_login_email = excluded.portal_login_email,
                updated_at = now();
            """,
            cancellationToken,
            transaction,
            new NpgsqlParameter<Guid>("id", profileId),
            new NpgsqlParameter<string>("nombre", nombre),
            new NpgsqlParameter<string>("email", email),
            new NpgsqlParameter<Guid>("authUserId", authUserId),
            new NpgsqlParameter<string>("portalLoginEmail", loginIdentifier));
    }

    private async Task ReplaceRolePermissionsAsync(long roleId, IReadOnlyCollection<string> permissionKeys, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        var keys = permissionKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await ExecuteAsync(
            "delete from public.rbac_role_permissions where role_id = @roleId;",
            cancellationToken,
            transaction,
            new NpgsqlParameter<long>("roleId", roleId));

        if (keys.Length == 0)
        {
            return;
        }

        await ExecuteAsync(
            """
            insert into public.rbac_role_permissions(role_id, permission_id, granted)
            select @roleId, p.id, true
            from public.rbac_permissions p
            where p.key = any(@permissionKeys);
            """,
            cancellationToken,
            transaction,
            new NpgsqlParameter<long>("roleId", roleId),
            CreateTextArrayParameter("permissionKeys", keys));
    }

    private async Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<string> roleSlugs, string primaryRoleSlug, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        var slugs = roleSlugs
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await ExecuteAsync(
            "delete from public.rbac_user_roles where user_id = @userId;",
            cancellationToken,
            transaction,
            new NpgsqlParameter("userId", userId));

        if (slugs.Length == 0)
        {
            return;
        }

        var primarySlug = primaryRoleSlug.Trim().ToLowerInvariant();
        await ExecuteAsync(
            """
            insert into public.rbac_user_roles (user_id, role_id, is_primary, assigned_by)
            select
                @userId,
                r.id,
                case when r.slug = @primarySlug then true else false end,
                null
            from public.rbac_roles r
            where r.slug = any(@roleSlugs)
              and r.activo = true;
            """,
            cancellationToken,
            transaction,
            new NpgsqlParameter("userId", userId),
            new NpgsqlParameter("primarySlug", primarySlug),
            CreateTextArrayParameter("roleSlugs", slugs));
    }

    private static NpgsqlParameter<string[]> CreateTextArrayParameter(string parameterName, string[] values)
    {
        return new NpgsqlParameter<string[]>(parameterName, values) { DataTypeName = "text[]" };
    }

    private async Task RebuildEffectivePermissionsForRoleAsync(long roleId, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        await ExecuteAsync(
            """
            delete from public.rbac_effective_permissions
            where user_id in (
                select distinct ur.user_id
                from public.rbac_user_roles ur
                where ur.role_id = @roleId
            );

            insert into public.rbac_effective_permissions (user_id, permission_key, source_role_id)
            select
                ur.user_id,
                perm.key,
                ur.role_id
            from public.rbac_user_roles ur
            join public.rbac_roles r on r.id = ur.role_id and r.activo = true
            join public.rbac_role_permissions rp on rp.role_id = ur.role_id and rp.granted = true
            join public.rbac_permissions perm on perm.id = rp.permission_id
            where ur.expires_at is null
              and ur.user_id in (
                  select distinct ur2.user_id
                  from public.rbac_user_roles ur2
                  where ur2.role_id = @roleId
              )
            group by ur.user_id, perm.key, ur.role_id;
            """,
            cancellationToken,
            transaction,
            new NpgsqlParameter<long>("roleId", roleId));
    }

    private async Task RebuildEffectivePermissionsForUserAsync(Guid userId, CancellationToken cancellationToken, IDbContextTransaction? transaction = null)
    {
        await ExecuteAsync(
            """
            delete from public.rbac_effective_permissions
            where user_id = @userId;

            insert into public.rbac_effective_permissions (user_id, permission_key, source_role_id)
            select
                ur.user_id,
                perm.key,
                ur.role_id
            from public.rbac_user_roles ur
            join public.rbac_roles r on r.id = ur.role_id and r.activo = true
            join public.rbac_role_permissions rp on rp.role_id = ur.role_id and rp.granted = true
            join public.rbac_permissions perm on perm.id = rp.permission_id
            where ur.expires_at is null
              and ur.user_id = @userId
            group by ur.user_id, perm.key, ur.role_id;
            """,
            cancellationToken,
            transaction,
            new NpgsqlParameter<Guid>("userId", userId));
    }

    private async Task<List<T>> QueryAsync<T>(string sql, Func<NpgsqlDataReader, T> map, CancellationToken cancellationToken, IDbContextTransaction? transaction = null, params NpgsqlParameter[] parameters)
    {
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
            if (transaction is not null)
            {
                command.Transaction = transaction.GetDbTransaction() as NpgsqlTransaction;
            }

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            var items = new List<T>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(map(reader));
            }

            return items;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, IDbContextTransaction? transaction = null, params NpgsqlParameter[] parameters)
    {
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
            if (transaction is not null)
            {
                command.Transaction = transaction.GetDbTransaction() as NpgsqlTransaction;
            }

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<T> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken, IDbContextTransaction? transaction = null, params NpgsqlParameter[] parameters)
    {
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
            if (transaction is not null)
            {
                command.Transaction = transaction.GetDbTransaction() as NpgsqlTransaction;
            }

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result is DBNull)
            {
                return default!;
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
