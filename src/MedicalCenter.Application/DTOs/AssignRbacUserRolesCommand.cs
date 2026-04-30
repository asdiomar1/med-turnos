namespace MedicalCenter.Application.DTOs;

public sealed record AssignRbacUserRolesCommand(
    Guid UserId,
    IReadOnlyCollection<string> RoleSlugs,
    string? PrimaryRoleSlug);
