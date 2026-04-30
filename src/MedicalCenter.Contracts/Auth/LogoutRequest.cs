using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class LogoutRequest
{
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = string.Empty;
}
