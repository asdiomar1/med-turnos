using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;

namespace MedicalCenter.Application.Features.Rbac;

public sealed class RbacService(
    IRbacAdminRepository rbacAdminRepository,
    IPasswordHasher passwordHasher) : IRbacService
{
    public Task<IReadOnlyCollection<RbacPermissionSummary>> ListPermissionsAsync(CancellationToken cancellationToken) =>
        rbacAdminRepository.ListPermissionsAsync(cancellationToken);

    public async Task<IReadOnlyCollection<RbacRoleSummary>> ListRolesAsync(CancellationToken cancellationToken) =>
        await rbacAdminRepository.ListRolesAsync(cancellationToken);

    public async Task<RbacRoleSummary> UpsertRoleAsync(UpsertRbacRoleCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Slug) || string.IsNullOrWhiteSpace(command.Nombre))
        {
            throw new ValidationException("slug y nombre son obligatorios");
        }

        return await rbacAdminRepository.UpsertRoleAsync(command, cancellationToken);
    }

    public async Task SetRolePermissionsAsync(string roleSlug, IReadOnlyCollection<string> permissionKeys, CancellationToken cancellationToken)
    {
        await rbacAdminRepository.SetRolePermissionsAsync(roleSlug, permissionKeys, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RbacStaffUserSummary>> ListStaffUsersAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        return await rbacAdminRepository.ListStaffUsersAsync(includeInactive, cancellationToken);
    }

    public async Task<RbacStaffUserSummary> CreateStaffUserAsync(string nombre, string? email, string identifier, string password, string roleSlug, bool primary, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(roleSlug))
        {
            throw new ValidationException("nombre, identifier, password y role_slug son obligatorios");
        }

        var role = await rbacAdminRepository.GetRoleBySlugAsync(roleSlug, cancellationToken) ?? throw new NotFoundException("Rol no encontrado");
        if (!role.IsStaff)
        {
            throw new ConflictException("Para staff no se permite rol paciente");
        }

        return await rbacAdminRepository.CreateStaffUserAsync(
            nombre,
            email,
            identifier,
            passwordHasher.Hash(password),
            role.Slug,
            primary,
            cancellationToken);
    }

    public async Task AssignUserRolesAsync(AssignRbacUserRolesCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ValidationException("user_id es obligatorio");
        }

        var roles = await ResolveRolesAsync(command.RoleSlugs, cancellationToken);
        if (roles.Count == 0)
        {
            throw new ValidationException("Debe seleccionar al menos un rol");
        }

        if (roles.Any(x => x.IsStaff) && roles.Any(x => !x.IsStaff))
        {
            throw new ConflictException("No se permite mezclar roles staff y paciente en la misma cuenta");
        }

        await rbacAdminRepository.AssignUserRolesAsync(command, cancellationToken);
    }

    public async Task SetStaffUserActiveAsync(SetStaffUserActiveCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ValidationException("user_id es obligatorio");
        }

        if (command.Active && !string.IsNullOrWhiteSpace(command.RoleSlug))
        {
            var role = await rbacAdminRepository.GetRoleBySlugAsync(command.RoleSlug, cancellationToken)
                       ?? throw new NotFoundException("Rol no encontrado");
            if (!role.IsStaff)
            {
                throw new ConflictException("Para staff no se permite rol paciente");
            }
        }

        await rbacAdminRepository.SetStaffUserActiveAsync(command, cancellationToken);
    }

    private async Task<List<RbacRoleSummary>> ResolveRolesAsync(IReadOnlyCollection<string> roleSlugs, CancellationToken cancellationToken)
    {
        var result = new List<RbacRoleSummary>();
        foreach (var slug in roleSlugs.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var role = await rbacAdminRepository.GetRoleBySlugAsync(slug, cancellationToken);
            if (role is null)
            {
                throw new NotFoundException($"Rol no encontrado: {slug}");
            }

            result.Add(role);
        }

        return result;
    }
}
