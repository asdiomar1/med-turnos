using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Configuration;

public sealed class DiasLaborablesConfigResponse
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("dias_semana")]
    public IReadOnlyCollection<short> DiasSemana { get; init; } = [];
}

public sealed class UpsertDiasLaborablesConfigRequest
{
    [JsonPropertyName("dias_semana")]
    public IReadOnlyCollection<short> DiasSemana { get; init; } = [];
}

public sealed class WhatsappMessageSettingResponse
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("message_text")]
    public string MessageText { get; init; } = string.Empty;

    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class UpdateWhatsappMessageSettingRequest
{
    [JsonPropertyName("message_text")]
    public string MessageText { get; init; } = string.Empty;

    [JsonPropertyName("active")]
    public bool Active { get; init; }
}
