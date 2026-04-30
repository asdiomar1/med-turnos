using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Auth;

public sealed class CreatePortalAccessTokenRequest
{
    [JsonPropertyName("paciente_id")]
    public Guid PacienteId { get; init; }

    [JsonPropertyName("purpose")]
    public string Purpose { get; init; } = string.Empty;

    [JsonPropertyName("delivery_channel")]
    public string DeliveryChannel { get; init; } = string.Empty;
}
