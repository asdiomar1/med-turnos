namespace MedicalCenter.Application.DTOs;

public sealed record StaffProfileSummary(
    Guid Id,
    string Identifier,
    string? Email,
    string? Nombre,
    bool IsActive,
    bool IsStaff,
    IReadOnlyCollection<string> Roles);
