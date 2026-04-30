using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Rbac;

public sealed class RbacPermissionResponse
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; init; }

    [JsonPropertyName("modulo")]
    public string Modulo { get; init; } = string.Empty;

    [JsonPropertyName("is_system")]
    public bool IsSystem { get; init; }
}

public sealed class RbacRoleResponse
{
    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; init; }

    [JsonPropertyName("activo")]
    public bool Activo { get; init; }

    [JsonPropertyName("is_system")]
    public bool IsSystem { get; init; }

    [JsonPropertyName("is_staff")]
    public bool IsStaff { get; init; }

    [JsonPropertyName("default_home")]
    public string DefaultHome { get; init; } = "/usuario";

    [JsonPropertyName("permissions")]
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}

public sealed class UpsertRbacRoleRequest
{
    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; init; }

    [JsonPropertyName("activo")]
    public bool Activo { get; init; } = true;

    [JsonPropertyName("is_system")]
    public bool IsSystem { get; init; } = false;

    [JsonPropertyName("is_staff")]
    public bool IsStaff { get; init; } = true;

    [JsonPropertyName("default_home")]
    public string DefaultHome { get; init; } = "/usuario";

    [JsonPropertyName("permissions")]
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}

public sealed class SetRbacRolePermissionsRequest
{
    [JsonPropertyName("permission_keys")]
    public IReadOnlyCollection<string> PermissionKeys { get; init; } = [];
}

public sealed class AssignRbacUserRolesRequest
{
    [JsonPropertyName("role_slugs")]
    public IReadOnlyCollection<string> RoleSlugs { get; init; } = [];

    [JsonPropertyName("primary_role_slug")]
    public string? PrimaryRoleSlug { get; init; }
}

public sealed class RbacStaffUserResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("auth_user_id")]
    public Guid? AuthUserId { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("roles")]
    public IReadOnlyCollection<string> Roles { get; init; } = [];

    [JsonPropertyName("primary_role")]
    public string? PrimaryRole { get; init; }
}

public sealed class CreateStaffUserRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("login_identifier")]
    public string Identifier { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("role_slug")]
    public string RoleSlug { get; init; } = string.Empty;

    [JsonPropertyName("primary")]
    public bool Primary { get; init; } = true;
}

public sealed class SetStaffUserActiveRequest
{
    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("role_slug")]
    public string? RoleSlug { get; init; }
}
