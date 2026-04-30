using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Common;

public sealed class OkResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }
}
