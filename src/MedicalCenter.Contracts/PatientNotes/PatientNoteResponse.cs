using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.PatientNotes;

public sealed class PatientNoteResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("paciente_id")]
    public Guid PatientId { get; init; }

    [JsonPropertyName("autor_id")]
    public Guid AuthorId { get; init; }

    [JsonPropertyName("autor_nombre")]
    public string? AuthorNombre { get; init; }

    [JsonPropertyName("mensaje")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreatePatientNoteRequest
{
    [JsonPropertyName("mensaje")]
    public string Mensaje { get; init; } = string.Empty;
}
