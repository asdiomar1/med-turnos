using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class PortalRecoveryResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("needs_manual_support")]
    public bool NeedsManualSupport { get; init; }
}
