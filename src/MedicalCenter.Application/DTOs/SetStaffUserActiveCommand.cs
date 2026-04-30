namespace MedicalCenter.Application.DTOs;

public sealed record SetStaffUserActiveCommand(
    Guid UserId,
    bool Active,
    string? RoleSlug);
