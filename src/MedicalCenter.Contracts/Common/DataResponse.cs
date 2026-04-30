using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Common;

public sealed class DataResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; init; } = default!;

    [JsonPropertyName("error")]
    public object? Error { get; init; }
}
