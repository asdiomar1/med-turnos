using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed record RoleCreateParams(
    Guid Id,
    string Code,
    string Name,
    IEnumerable<string>? Permissions = null,
    string? Description = null,
    bool Active = true,
    bool IsSystem = false,
    bool IsStaff = true,
    string DefaultHome = "/usuario");

public sealed class Role : Entity<Guid>
{
    private Role() { }

    public Role(RoleCreateParams p)
    {
        Id = p.Id;
        Code = p.Code;
        Name = p.Name;
        Permissions = p.Permissions?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        Description = p.Description;
        Active = p.Active;
        IsSystem = p.IsSystem;
        IsStaff = p.IsStaff;
        DefaultHome = p.DefaultHome;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool Active { get; private set; } = true;
    public bool IsSystem { get; private set; }
    public bool IsStaff { get; private set; } = true;
    public string DefaultHome { get; private set; } = "/usuario";
    public List<string> Permissions { get; private set; } = [];

    public void UpdateMetadata(string name, string? description, bool active, bool isSystem, bool isStaff, string defaultHome)
    {
        Name = name;
        Description = description;
        Active = active;
        IsSystem = isSystem;
        IsStaff = isStaff;
        DefaultHome = string.IsNullOrWhiteSpace(defaultHome) ? "/usuario" : defaultHome.Trim();
    }

    public void SetPermissions(IEnumerable<string> permissions)
    {
        Permissions = permissions?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
    }
}
