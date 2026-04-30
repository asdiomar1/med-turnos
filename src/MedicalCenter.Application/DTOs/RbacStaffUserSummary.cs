namespace MedicalCenter.Application.DTOs;

public sealed record RbacStaffUserSummary(
    Guid Id,
    string? Nombre,
    string Email,
    Guid? AuthUserId,
    bool IsActive,
    IReadOnlyCollection<string> Roles,
    string? PrimaryRole);
