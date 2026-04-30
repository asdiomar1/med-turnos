namespace MedicalCenter.Application.DTOs;

public sealed record EffectiveAccessResult(
    Guid ProfileId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> EffectivePermissions,
    string PrimaryRole,
    string DefaultHome,
    bool IsStaff);
