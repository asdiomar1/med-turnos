using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Dashboards;

public sealed class DashboardSummaryResponse
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

    [JsonPropertyName("generado_en")]
    public DateTimeOffset GeneradoEn { get; init; }
}

public sealed class DashboardOccupancyCameraResponse
{
    [JsonPropertyName("camera_id")]
    public int? CameraId { get; init; }

    [JsonPropertyName("camera_name")]
    public string? CameraName { get; init; }

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
}

public sealed class DashboardOccupancyResponse
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

    [JsonPropertyName("por_camara")]
    public IReadOnlyCollection<DashboardOccupancyCameraResponse> PorCamara { get; init; } = [];
}

public sealed class DashboardAgendaBucketResponse
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("camera_id")]
    public int? CameraId { get; init; }

    [JsonPropertyName("camera_name")]
    public string? CameraName { get; init; }

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
}

public sealed class DashboardAlertResponse
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = "info";

    [JsonPropertyName("count")]
    public int Count { get; init; }
}

public sealed class DashboardWeeklyVolumeItemResponse
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
}
