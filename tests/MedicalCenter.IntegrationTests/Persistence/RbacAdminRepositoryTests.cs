using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Caching;
using MedicalCenter.Infrastructure.Persistence;
using MedicalCenter.Infrastructure.Persistence.Repositories;
using MedicalCenter.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace MedicalCenter.IntegrationTests.Persistence;

public sealed class RbacAdminRepositoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RbacAdminRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private DbContextOptions<MedicalCenterDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;

    private class PassThroughCache : ICacheService
    {
        public HashSet<string> RemovedKeys { get; } = new();

        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
            => factory()!;
            
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) 
        {
            RemovedKeys.Add(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
    }

    private RbacAdminRepository MakeRepo(MedicalCenterDbContext ctx, ICacheService? cache = null) =>
        new(ctx, cache ?? new PassThroughCache());

    // --- Data Seeding Helpers ---

    private async Task SeedPermissionAsync(MedicalCenterDbContext ctx, string key, string name)
    {
        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO public.rbac_permissions (key, nombre, modulo, is_system) VALUES ({0}, {1}, 'test', true) ON CONFLICT (key) DO NOTHING;",
            key, name);
    }

    private async Task<long> SeedRoleAsync(MedicalCenterDbContext ctx, string slug, string name, bool isSystem = false, bool isStaff = true)
    {
        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO public.rbac_roles (slug, nombre, descripcion, activo, is_system, is_staff, default_home) VALUES ({0}, {1}, 'test', true, {2}, {3}, '/test') ON CONFLICT (slug) DO UPDATE SET activo = true;",
            slug, name, isSystem, isStaff);
        
        var id = await ctx.Database.SqlQueryRaw<long>("SELECT id as \"Value\" FROM public.rbac_roles WHERE slug = {0}", slug).FirstOrDefaultAsync();
        return id;
    }

    private async Task SeedRolePermissionAsync(MedicalCenterDbContext ctx, long roleId, string permissionKey)
    {
        var permId = await ctx.Database.SqlQueryRaw<long>("SELECT id as \"Value\" FROM public.rbac_permissions WHERE key = {0}", permissionKey).FirstOrDefaultAsync();
        if (permId > 0)
        {
            await ctx.Database.ExecuteSqlRawAsync(
                "INSERT INTO public.rbac_role_permissions (role_id, permission_id, granted) VALUES ({0}, {1}, true) ON CONFLICT DO NOTHING;",
                roleId, permId);
        }
    }

    // --- Tests: Permissions and Roles Retrieval ---

    [Fact]
    public async Task ListPermissionsAsync_ReturnsAllPermissions()
    {
        var key1 = $"perm-{Guid.NewGuid():N}";
        var key2 = $"perm-{Guid.NewGuid():N}";
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedPermissionAsync(ctx, key1, "Perm 1");
            await SeedPermissionAsync(ctx, key2, "Perm 2");
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repo = MakeRepo(qCtx);
        var result = await repo.ListPermissionsAsync(CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Contains(result, p => p.Key == key1);
        Assert.Contains(result, p => p.Key == key2);
    }

    [Fact]
    public async Task ListRolesAsync_ReturnsRolesWithPermissions()
    {
        var roleSlug = $"role-{Guid.NewGuid():N}";
        var permKey = $"perm-{Guid.NewGuid():N}";
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedPermissionAsync(ctx, permKey, "Perm");
            var roleId = await SeedRoleAsync(ctx, roleSlug, "Role");
            await SeedRolePermissionAsync(ctx, roleId, permKey);
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repo = MakeRepo(qCtx);
        var result = await repo.ListRolesAsync(CancellationToken.None);

        var role = Assert.Single(result, r => r.Slug == roleSlug);
        Assert.Contains(permKey, role.Permissions);
    }

    [Fact]
    public async Task GetRoleBySlugAsync_ValidSlug_ReturnsRole()
    {
        var slug = $"role-{Guid.NewGuid():N}";
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedRoleAsync(ctx, slug, "Role test");
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repo = MakeRepo(qCtx);
        var role = await repo.GetRoleBySlugAsync(slug.ToUpperInvariant(), CancellationToken.None); // Case insensitive test

        Assert.NotNull(role);
        Assert.Equal(slug, role.Slug);
    }

    // --- Tests: Role Management ---

    [Fact]
    public async Task UpsertRoleAsync_NewRole_CreatesRoleAndAssignsPermissions()
    {
        var slug = $"role-{Guid.NewGuid():N}";
        var permKey = $"perm-{Guid.NewGuid():N}";
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedPermissionAsync(ctx, permKey, "Perm");
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repo = MakeRepo(qCtx);
        var command = new UpsertRbacRoleCommand(slug, "New Role", "Desc", true, false, true, "/home", [permKey]);
        
        var result = await repo.UpsertRoleAsync(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
        Assert.Contains(permKey, result.Permissions);
    }

    [Fact]
    public async Task SetRolePermissionsAsync_ValidRole_ReplacesPermissionsAndClearsCache()
    {
        var slug = $"role-{Guid.NewGuid():N}";
        var permOld = $"perm-{Guid.NewGuid():N}";
        var permNew = $"perm-{Guid.NewGuid():N}";
        var opts = CreateOptions();
        var cacheMock = new PassThroughCache();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedPermissionAsync(ctx, permOld, "Old Perm");
            await SeedPermissionAsync(ctx, permNew, "New Perm");
            var roleId = await SeedRoleAsync(ctx, slug, "Role");
            await SeedRolePermissionAsync(ctx, roleId, permOld);
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repo = MakeRepo(qCtx, cacheMock);
        
        await repo.SetRolePermissionsAsync(slug, [permNew], CancellationToken.None);

        var updatedRole = await repo.GetRoleBySlugAsync(slug, CancellationToken.None);
        Assert.NotNull(updatedRole);
        Assert.Contains(permNew, updatedRole.Permissions);
        Assert.DoesNotContain(permOld, updatedRole.Permissions);
        
        Assert.Contains("mc:rbac:roles:admin", cacheMock.RemovedKeys);
    }

    // --- Tests: Staff Management ---

    [Fact]
    public async Task CreateStaffUserAsync_ValidData_CreatesUserProfileAndRole()
    {
        var identifier = $"usr-{Guid.NewGuid():N}";
        var roleSlug = $"role-{Guid.NewGuid():N}";
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedRoleAsync(ctx, roleSlug, "Role");
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repo = MakeRepo(qCtx);

        var result = await repo.CreateStaffUserAsync("John Doe", $"{identifier}@test.com", identifier, "hash", roleSlug, true, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Nombre);
        Assert.Contains(roleSlug, result.Roles);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task ListStaffUsersAsync_ExcludeInactive_FiltersOutInactiveRoles()
    {
        var authUserId1 = Guid.NewGuid();
        var authUserId2 = Guid.NewGuid();
        var activeRole = $"role-active-{Guid.NewGuid():N}";
        var inactiveRole = "staff_inactivo";
        var opts = CreateOptions();

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedRoleAsync(ctx, activeRole, "Active");
            await SeedRoleAsync(ctx, inactiveRole, "Inactivo"); // Ensure staff_inactivo exists
        }

        await using (var setupCtx = new MedicalCenterDbContext(opts))
        {
            var repo = MakeRepo(setupCtx);
            await repo.CreateStaffUserAsync("Active User", null, $"usr-{Guid.NewGuid():N}", "h", activeRole, true, CancellationToken.None);
            await repo.CreateStaffUserAsync("Inactive User", null, $"usr-{Guid.NewGuid():N}", "h", inactiveRole, true, CancellationToken.None);
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repoQuery = MakeRepo(qCtx);
        var activeStaff = await repoQuery.ListStaffUsersAsync(includeInactive: false, CancellationToken.None);
        var allStaff = await repoQuery.ListStaffUsersAsync(includeInactive: true, CancellationToken.None);

        Assert.Contains(activeStaff, s => s.Nombre == "Active User");
        Assert.DoesNotContain(activeStaff, s => s.Nombre == "Inactive User");
        Assert.Contains(allStaff, s => s.Nombre == "Inactive User");
    }

    [Fact]
    public async Task AssignUserRolesAsync_UpdatesRolesAndPrimaryRole()
    {
        var role1 = $"role-{Guid.NewGuid():N}";
        var role2 = $"role-{Guid.NewGuid():N}";
        var opts = CreateOptions();
        RbacStaffUserSummary user;

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedRoleAsync(ctx, role1, "Role 1");
            await SeedRoleAsync(ctx, role2, "Role 2");
            var setupRepo = MakeRepo(ctx);
            user = await setupRepo.CreateStaffUserAsync("User", null, $"usr-{Guid.NewGuid():N}", "h", role1, true, CancellationToken.None);
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repo = MakeRepo(qCtx);
        
        await repo.AssignUserRolesAsync(new AssignRbacUserRolesCommand(user.Id, [role1, role2], role2), CancellationToken.None);
        
        var staffList = await repo.ListStaffUsersAsync(true, CancellationToken.None);
        var updatedUser = staffList.Single(u => u.Id == user.Id);
        
        Assert.Contains(role1, updatedUser.Roles);
        Assert.Contains(role2, updatedUser.Roles);
        Assert.Equal(role2, updatedUser.PrimaryRole);
    }

    [Fact]
    public async Task SetStaffUserActiveAsync_Deactivate_SetsRoleToInactivo()
    {
        var roleSlug = $"role-{Guid.NewGuid():N}";
        var inactiveRole = "staff_inactivo";
        var opts = CreateOptions();
        RbacStaffUserSummary user;

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedRoleAsync(ctx, roleSlug, "Role");
            await SeedRoleAsync(ctx, inactiveRole, "Inactivo");
            var setupRepo = MakeRepo(ctx);
            user = await setupRepo.CreateStaffUserAsync("User", null, $"usr-{Guid.NewGuid():N}", "h", roleSlug, true, CancellationToken.None);
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repo = MakeRepo(qCtx);
        
        await repo.SetStaffUserActiveAsync(new SetStaffUserActiveCommand(user.Id, false, inactiveRole), CancellationToken.None);

        var staffList = await repo.ListStaffUsersAsync(true, CancellationToken.None);
        var updatedUser = staffList.Single(u => u.Id == user.Id);
        
        Assert.False(updatedUser.IsActive);
        Assert.Contains(inactiveRole, updatedUser.Roles);
    }

    [Fact]
    public async Task UpdateMyDataAsync_ValidData_UpdatesUserAndProfile()
    {
        var roleSlug = $"role-{Guid.NewGuid():N}";
        var opts = CreateOptions();
        RbacStaffUserSummary user;

        await using (var ctx = new MedicalCenterDbContext(opts))
        {
            await ctx.Database.MigrateAsync();
            await DatabaseInitializer.EnsureRbacSchemaAsync(ctx);
            await SeedRoleAsync(ctx, roleSlug, "Role");
            
            var setupRepo = MakeRepo(ctx);
            user = await setupRepo.CreateStaffUserAsync("Old Name", null, $"usr-{Guid.NewGuid():N}", "h", roleSlug, true, CancellationToken.None);
        }

        await using var qCtx = new MedicalCenterDbContext(opts);
        var repo = MakeRepo(qCtx);
        
        Assert.NotNull(user.AuthUserId);
        
        var result = await repo.UpdateMyDataAsync(user.AuthUserId.Value, "New Name", CancellationToken.None);

        Assert.Equal("New Name", result.Nombre);

        var staffList = await repo.ListStaffUsersAsync(true, CancellationToken.None);
        var updatedUser = staffList.Single(u => u.Id == user.Id);
        Assert.Equal("New Name", updatedUser.Nombre);
    }
}
