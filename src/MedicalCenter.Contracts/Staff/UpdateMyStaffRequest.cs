using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Staff;

public sealed class UpdateMyStaffRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
}
