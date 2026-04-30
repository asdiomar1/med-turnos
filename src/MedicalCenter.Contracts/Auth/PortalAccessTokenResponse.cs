using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class PortalAccessTokenResponse
{
    [JsonPropertyName("token_id")]
    public Guid TokenId { get; init; }

    [JsonPropertyName("purpose")]
    public string Purpose { get; init; } = string.Empty;

    [JsonPropertyName("delivery_channel")]
    public string DeliveryChannel { get; init; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("token_plain")]
    public string? TokenPlain { get; init; }
}
