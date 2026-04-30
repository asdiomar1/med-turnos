using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Appointments;

public sealed class CancelBlockAppointmentsRequest
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("camara_id")]
    public int CamaraId { get; init; }

    [JsonPropertyName("paciente_id")]
    public Guid PacienteId { get; init; }

    [JsonPropertyName("motivo")]
    public string? Motivo { get; init; }
}
