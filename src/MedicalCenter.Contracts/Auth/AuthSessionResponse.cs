using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class AuthSessionResponse
{
    [JsonPropertyName("session")]
    public AuthTokenResponse Session { get; init; } = new();

    [JsonPropertyName("user")]
    public AuthUserResponse User { get; init; } = new();
}

public sealed class AuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "bearer";
}

public sealed class AuthUserResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
