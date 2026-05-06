using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class ChangeMyPasswordRequest
{
    [JsonPropertyName("current_password")]
    public string CurrentPassword { get; init; } = string.Empty;

    [JsonPropertyName("new_password")]
    public string NewPassword { get; init; } = string.Empty;
}
