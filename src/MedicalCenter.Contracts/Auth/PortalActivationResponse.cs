using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class PortalActivationResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("login_identifier")]
    public string LoginIdentifier { get; init; } = string.Empty;
}
