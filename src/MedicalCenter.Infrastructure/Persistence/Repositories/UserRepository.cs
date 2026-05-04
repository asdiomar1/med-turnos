using System.Data;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MedicalCenter.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(MedicalCenterDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(identifier)
            ? Task.FromResult<User?>(null)
            : dbContext.Users.FirstOrDefaultAsync(
                x => x.Identifier.ToLower() == identifier.Trim().ToLower(),
                cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(email)
            ? Task.FromResult<User?>(null)
            : dbContext.Users.FirstOrDefaultAsync(
                x => x.Email.ToLower() == email.Trim().ToLower(),
                cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is not null)
        {
            var roles = await LoadRolesAsync(id, cancellationToken);
            user.SetRoles(roles);
        }
        return user;
    }

    public async Task<User?> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PatientId == patientId, cancellationToken);
        if (user is not null)
        {
            var roles = await LoadRolesAsync(user.Id, cancellationToken);
            user.SetRoles(roles);
        }
        return user;
    }

    public async Task<Guid?> GetProfileIdByAuthUserIdAsync(Guid authUserId, CancellationToken cancellationToken)
    {
        var profileId = await dbContext.Database.SqlQueryRaw<Guid>(
            """
            select id as "Value"
            from public.perfiles
            where auth_user_id = {0} or id = {0}
            order by case when auth_user_id = {0} then 0 else 1 end
            limit 1
            """,
            authUserId)
            .FirstOrDefaultAsync(cancellationToken);

        return profileId == Guid.Empty ? null : profileId;
    }

    public async Task<string?> GetDisplayNameByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profileName = await dbContext.Database.SqlQueryRaw<string>(
            """
            select p.nombre as "Value"
            from public.perfiles p
            where p.auth_user_id = {0} or p.id = {0}
            order by case when p.auth_user_id = {0} then 0 else 1 end
            limit 1
            """,
            userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(profileName))
        {
            return profileName;
        }

        var user = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new { x.Nombre, x.Identifier })
            .FirstOrDefaultAsync(cancellationToken);

        return user?.Nombre ?? user?.Identifier;
    }

    public async Task<IReadOnlyCollection<User>> GetStaffAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.Where(x => x.IsStaff);
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query.OrderBy(x => x.Nombre ?? x.Identifier).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetByRoleAsync(string roleCode, bool onlyActive, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
            return [];

        var users = await dbContext.Users
            .FromSqlRaw(
                """
                SELECT DISTINCT
                       u."Id",
                       u."Identifier",
                       u."Email",
                       u."PasswordHash",
                       u."IsActive",
                       u."IsStaff",
                       u."PatientId",
                       u."Nombre"
                FROM public.users u
                JOIN public.perfiles pf ON pf.auth_user_id = u."Id" OR pf.id = u."Id"
                JOIN public.rbac_user_roles ur ON ur.user_id = pf.id AND ur.expires_at IS NULL
                JOIN public.rbac_roles r ON r.id = ur.role_id AND r.activo = true
                WHERE r.slug = {0}
                """,
                roleCode.Trim())
            .Where(u => !onlyActive || u.IsActive)
            .OrderBy(u => u.Nombre ?? u.Identifier)
            .ToListAsync(cancellationToken);

        return users;
    }

    public Task AddAsync(User user, CancellationToken cancellationToken) =>
        dbContext.Users.AddAsync(user, cancellationToken).AsTask();

    public async Task<IReadOnlyCollection<User>> GetBasicByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var distinctIds = ids.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return [];
        }

        // Plain ToListAsync without role loading — keeps the query lightweight
        return await dbContext.Users
            .Where(x => distinctIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<Role>> LoadRolesAsync(Guid userId, CancellationToken cancellationToken)
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

        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new NpgsqlParameter<Guid>("userId", userId));

            var roles = new List<Role>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var permissions = reader.IsDBNull(7) ? [] : reader.GetFieldValue<string[]>(7);
                roles.Add(new Role(
                    Guid.NewGuid(),
                    reader.GetString(0),
                    reader.GetString(1),
                    permissions,
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetBoolean(3),
                    reader.GetBoolean(4),
                    reader.GetBoolean(5),
                    reader.GetString(6)));
            }
            return roles;
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }
}
