using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class Role : Entity<Guid>
{
    private Role() { }

    public Role(
        Guid id,
        string code,
        string name,
        IEnumerable<string>? permissions = null,
        string? description = null,
        bool active = true,
        bool isSystem = false,
        bool isStaff = true,
        string defaultHome = "/usuario")
    {
        Id = id;
        Code = code;
        Name = name;
        Permissions = permissions?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [];
        Description = description;
        Active = active;
        IsSystem = isSystem;
        IsStaff = isStaff;
        DefaultHome = defaultHome;
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
