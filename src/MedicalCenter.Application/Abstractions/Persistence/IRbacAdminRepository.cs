using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Abstractions.Persistence;

public interface IRbacAdminRepository
{
    Task<IReadOnlyCollection<RbacPermissionSummary>> ListPermissionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RbacRoleSummary>> ListRolesAsync(CancellationToken cancellationToken);
    Task<RbacRoleSummary?> GetRoleBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<RbacRoleSummary> UpsertRoleAsync(UpsertRbacRoleCommand command, CancellationToken cancellationToken);
    Task SetRolePermissionsAsync(string roleSlug, IReadOnlyCollection<string> permissionKeys, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RbacStaffUserSummary>> ListStaffUsersAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<RbacStaffUserSummary> CreateStaffUserAsync(
        string nombre,
        string? email,
        string identifier,
        string passwordHash,
        string roleSlug,
        bool primary,
        CancellationToken cancellationToken);
    Task AssignUserRolesAsync(AssignRbacUserRolesCommand command, CancellationToken cancellationToken);
    Task SetStaffUserActiveAsync(SetStaffUserActiveCommand command, CancellationToken cancellationToken);
    Task<StaffProfileSummary> UpdateMyDataAsync(Guid authUserId, string nombre, CancellationToken cancellationToken);
}
