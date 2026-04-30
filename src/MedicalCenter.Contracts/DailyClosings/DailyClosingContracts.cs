using System.Text.Json;
using System.Text.Json.Serialization;
using MedicalCenter.Contracts.Dashboards;

namespace MedicalCenter.Contracts.DailyClosings;

public sealed class DailyClosingPreviewResponse
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("total_turnos")]
    public int TotalTurnos { get; init; }

    [JsonPropertyName("libres")]
    public int Libres { get; init; }

    [JsonPropertyName("ocupados")]
    public int Ocupados { get; init; }

    [JsonPropertyName("apartados")]
    public int Apartados { get; init; }

    [JsonPropertyName("cancelados")]
    public int Cancelados { get; init; }

    [JsonPropertyName("ocupacion_porcentaje")]
    public decimal OcupacionPorcentaje { get; init; }

    [JsonPropertyName("apto_para_cierre")]
    public bool AptoParaCierre { get; init; }

    [JsonPropertyName("alertas")]
    public IReadOnlyCollection<DashboardAlertResponse> Alertas { get; init; } = [];

    [JsonPropertyName("generado_en")]
    public DateTimeOffset GeneradoEn { get; init; }
}

public sealed class DailyClosingResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("detalles")]
    public JsonElement? Detalles { get; init; }

    [JsonPropertyName("created_by_user_id")]
    public Guid CreatedByUserId { get; init; }

    [JsonPropertyName("confirmed_by_user_id")]
    public Guid? ConfirmedByUserId { get; init; }

    [JsonPropertyName("reopened_by_user_id")]
    public Guid? ReopenedByUserId { get; init; }

    [JsonPropertyName("motivo_reapertura")]
    public string? MotivoReapertura { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("confirmed_at")]
    public DateTimeOffset? ConfirmedAt { get; init; }

    [JsonPropertyName("reopened_at")]
    public DateTimeOffset? ReopenedAt { get; init; }
}

public sealed class ConfirmDailyClosingRequest
{
    [JsonPropertyName("detalles")]
    public JsonElement? Detalles { get; init; }
}

public sealed class ReopenDailyClosingRequest
{
    [JsonPropertyName("motivo")]
    public string? Motivo { get; init; }
}

public sealed class PreviewDailyClosingRequest
{
    [JsonPropertyName("fecha")]
    public DateOnly? Fecha { get; init; }
}
