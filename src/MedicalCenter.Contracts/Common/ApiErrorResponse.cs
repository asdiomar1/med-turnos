using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Common;

public sealed class ApiErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("details")]
    public object? Details { get; init; }
}
