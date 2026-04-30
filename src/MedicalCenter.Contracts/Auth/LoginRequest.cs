using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class LoginRequest
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}
