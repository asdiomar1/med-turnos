using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Dashboards;

public sealed class DashboardSummaryResponse
{
    [JsonPropertyName("pacientes_hoy")]
    public int PacientesHoy { get; init; }

    [JsonPropertyName("apartados_activos")]
    public int ApartadosActivos { get; init; }
}

public sealed class DashboardOccupancyCameraResponse
{
    [JsonPropertyName("camara_id")]
    public int CamaraId { get; init; }

    [JsonPropertyName("camara_nombre")]
    public string CamaraNombre { get; init; } = string.Empty;

    [JsonPropertyName("capacidad_total")]
    public int CapacidadTotal { get; init; }

    [JsonPropertyName("ocupados")]
    public int Ocupados { get; init; }

    [JsonPropertyName("porcentaje_ocupacion")]
    public int PorcentajeOcupacion { get; init; }
}

public sealed class DashboardAgendaRowResponse
{
    [JsonPropertyName("hora")]
    public string Hora { get; init; } = string.Empty;

    [JsonPropertyName("lugar")]
    public int Lugar { get; init; }

    [JsonPropertyName("camara_id")]
    public int CamaraId { get; init; }

    [JsonPropertyName("camara_nombre")]
    public string CamaraNombre { get; init; } = string.Empty;

    [JsonPropertyName("nombre_paciente")]
    public string NombrePaciente { get; init; } = string.Empty;

    [JsonPropertyName("modalidad_cobro")]
    public string ModalidadCobro { get; init; } = string.Empty;

    [JsonPropertyName("es_nuevo_ingreso")]
    public bool EsNuevoIngreso { get; init; }

    [JsonPropertyName("es_bloque_completo")]
    public bool EsBloqueCompleto { get; init; }

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;
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

public sealed class DashboardUiAlertResponse
{
    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;

    [JsonPropertyName("titulo")]
    public string Titulo { get; init; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; init; } = string.Empty;

    [JsonPropertyName("target_tab")]
    public string TargetTab { get; init; } = string.Empty;
}

public sealed class DashboardWeeklyVolumeItemResponse
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("ocupados")]
    public int Ocupados { get; init; }
}
