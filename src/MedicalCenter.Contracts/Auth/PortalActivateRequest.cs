using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class PortalActivateRequest
{
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;

    [JsonPropertyName("login_identifier")]
    public string LoginIdentifier { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}
