using MedicalCenter.Application.Features.Rbac;
using MedicalCenter.Contracts.Rbac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/rbac")]
[Authorize]
public sealed class RbacController(IRbacService rbacService) : ControllerBase
{
    [HttpGet("permissions")]
    [Authorize(Policy = "RbacRead")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken) =>
        Ok((await rbacService.ListPermissionsAsync(cancellationToken)).Select(Map));

    [HttpGet("roles")]
    [Authorize(Policy = "RbacRead")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken) =>
        Ok((await rbacService.ListRolesAsync(cancellationToken)).Select(Map));

    [HttpPost("roles")]
    [Authorize(Policy = "RbacManage")]
    public async Task<IActionResult> UpsertRole([FromBody] UpsertRbacRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await rbacService.UpsertRoleAsync(new MedicalCenter.Application.DTOs.UpsertRbacRoleCommand(
            request.Slug,
            request.Nombre,
            request.Descripcion,
            request.Activo,
            request.IsSystem,
            request.IsStaff,
            request.DefaultHome,
            request.Permissions), cancellationToken);

        return Ok(Map(result));
    }

    [HttpPut("roles/{roleSlug}/permissions")]
    [Authorize(Policy = "RbacManage")]
    public async Task<IActionResult> SetRolePermissions(string roleSlug, [FromBody] SetRbacRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        await rbacService.SetRolePermissionsAsync(roleSlug, request.PermissionKeys, cancellationToken);
        return NoContent();
    }

    [HttpGet("staff-users")]
    [Authorize(Policy = "StaffRead")]
    public async Task<IActionResult> GetStaffUsers([FromQuery(Name = "include_inactive")] bool includeInactive = false, CancellationToken cancellationToken = default) =>
        Ok((await rbacService.ListStaffUsersAsync(includeInactive, cancellationToken)).Select(Map));

    [HttpPost("staff-users")]
    [Authorize(Policy = "StaffManage")]
    public async Task<IActionResult> CreateStaffUser([FromBody] CreateStaffUserRequest request, CancellationToken cancellationToken)
    {
        var result = await rbacService.CreateStaffUserAsync(request.Nombre, request.Email, request.Identifier, request.Password, request.RoleSlug, request.Primary, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Map(result));
    }

    [HttpPut("staff-users/{userId:guid}/roles")]
    [Authorize(Policy = "StaffManage")]
    public async Task<IActionResult> AssignUserRoles(Guid userId, [FromBody] AssignRbacUserRolesRequest request, CancellationToken cancellationToken)
    {
        await rbacService.AssignUserRolesAsync(new MedicalCenter.Application.DTOs.AssignRbacUserRolesCommand(userId, request.RoleSlugs, request.PrimaryRoleSlug), cancellationToken);
        return NoContent();
    }

    [HttpPatch("staff-users/{userId:guid}/active")]
    [Authorize(Policy = "StaffManage")]
    public async Task<IActionResult> SetUserActive(Guid userId, [FromBody] SetStaffUserActiveRequest request, CancellationToken cancellationToken)
    {
        await rbacService.SetStaffUserActiveAsync(new MedicalCenter.Application.DTOs.SetStaffUserActiveCommand(userId, request.Active, request.RoleSlug), cancellationToken);
        return NoContent();
    }

    private static RbacPermissionResponse Map(MedicalCenter.Application.DTOs.RbacPermissionSummary x) => new()
    {
        Key = x.Key,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Modulo = x.Modulo,
        IsSystem = x.IsSystem
    };

    private static RbacRoleResponse Map(MedicalCenter.Application.DTOs.RbacRoleSummary x) => new()
    {
        Slug = x.Slug,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Activo = x.Activo,
        IsSystem = x.IsSystem,
        IsStaff = x.IsStaff,
        DefaultHome = x.DefaultHome,
        Permissions = x.Permissions
    };

    private static RbacStaffUserResponse Map(MedicalCenter.Application.DTOs.RbacStaffUserSummary x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Email = x.Email,
        AuthUserId = x.AuthUserId,
        IsActive = x.IsActive,
        Roles = x.Roles,
        PrimaryRole = x.PrimaryRole
    };
}
