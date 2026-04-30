using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class PortalRecoveryRequest
{
    [JsonPropertyName("documento_identidad")]
    public string DocumentoIdentidad { get; init; } = string.Empty;
}
