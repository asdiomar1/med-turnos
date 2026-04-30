using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class EffectiveAccessResponse
{
    [JsonPropertyName("profile_id")]
    public Guid ProfileId { get; init; }

    [JsonPropertyName("roles")]
    public IReadOnlyCollection<string> Roles { get; init; } = [];

    [JsonPropertyName("effective_permissions")]
    public IReadOnlyCollection<string> EffectivePermissions { get; init; } = [];

    [JsonPropertyName("primary_role")]
    public string PrimaryRole { get; init; } = string.Empty;

    [JsonPropertyName("default_home")]
    public string DefaultHome { get; init; } = string.Empty;

    [JsonPropertyName("is_staff")]
    public bool IsStaff { get; init; }
}
