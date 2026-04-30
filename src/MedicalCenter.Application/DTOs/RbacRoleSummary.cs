namespace MedicalCenter.Application.DTOs;

public sealed record RbacRoleSummary(
    string Slug,
    string Nombre,
    string? Descripcion,
    bool Activo,
    bool IsSystem,
    bool IsStaff,
    string DefaultHome,
    IReadOnlyCollection<string> Permissions);
