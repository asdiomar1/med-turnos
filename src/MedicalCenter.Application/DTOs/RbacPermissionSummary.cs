namespace MedicalCenter.Application.DTOs;

public sealed record RbacPermissionSummary(
    string Key,
    string Nombre,
    string? Descripcion,
    string Modulo,
    bool IsSystem);
