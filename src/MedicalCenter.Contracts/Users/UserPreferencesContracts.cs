using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Users;

public sealed class UserPreferencesResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("theme")]
    public string Theme { get; init; } = "system";

    [JsonPropertyName("custom_colors")]
    public JsonElement? CustomColors { get; init; }

    [JsonPropertyName("turnos_layout")]
    public string TurnosLayout { get; init; } = "standard";

    [JsonPropertyName("font_scale")]
    public decimal FontScale { get; init; } = 1.0m;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class UpdateUserPreferencesRequest
{
    [JsonPropertyName("theme")]
    public string? Theme { get; init; }

    [JsonPropertyName("custom_colors")]
    public JsonElement? CustomColors { get; init; }

    [JsonPropertyName("turnos_layout")]
    public string? TurnosLayout { get; init; }

    [JsonPropertyName("font_scale")]
    public decimal? FontScale { get; init; }
}
