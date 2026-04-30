using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Imports;

public sealed class CreateImportUploadUrlRequest
{
    [JsonPropertyName("file_name")]
    public string FileName { get; init; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; init; } = string.Empty;
}

public sealed class CreateImportUploadUrlResponse
{
    [JsonPropertyName("importacion_id")]
    public Guid ImportacionId { get; init; }

    [JsonPropertyName("upload_url")]
    public string UploadUrl { get; init; } = string.Empty;

    [JsonPropertyName("storage_key")]
    public string StorageKey { get; init; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("required_headers")]
    public IReadOnlyDictionary<string, string> RequiredHeaders { get; init; } = new Dictionary<string, string>();
}

public sealed class ConfirmImportResponse
{
    [JsonPropertyName("importacion_id")]
    public Guid ImportacionId { get; init; }

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("total_rows")]
    public int TotalRows { get; init; }

    [JsonPropertyName("created_rows")]
    public int CreatedRows { get; init; }

    [JsonPropertyName("updated_rows")]
    public int UpdatedRows { get; init; }

    [JsonPropertyName("skipped_rows")]
    public int SkippedRows { get; init; }

    [JsonPropertyName("error_rows")]
    public int ErrorRows { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyCollection<ImportPatientRowErrorResponse> Errors { get; init; } = [];
}

public sealed class ImportSummaryResponse
{
    [JsonPropertyName("importacion_id")]
    public Guid ImportacionId { get; init; }

    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; init; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; init; }

    [JsonPropertyName("total_filas")]
    public int TotalFilas { get; init; }

    [JsonPropertyName("filas_insertadas")]
    public int FilasInsertadas { get; init; }

    [JsonPropertyName("filas_actualizadas")]
    public int FilasActualizadas { get; init; }

    [JsonPropertyName("filas_con_error")]
    public int FilasConError { get; init; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("started_at")]
    public DateTimeOffset? StartedAt { get; init; }

    [JsonPropertyName("finished_at")]
    public DateTimeOffset? FinishedAt { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyCollection<ImportPatientRowErrorResponse> Errors { get; init; } = [];
}

public sealed class ImportPatientRowErrorResponse
{
    [JsonPropertyName("row_number")]
    public int RowNumber { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

// Legacy — kept for backward-compat with IFormFile upload path
public sealed class ImportPatientsRequest
{
    [JsonPropertyName("storage_path")]
    public string? StoragePath { get; init; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }
}

public sealed class ImportPatientsResponse
{
    [JsonPropertyName("total_rows")]
    public int TotalRows { get; init; }

    [JsonPropertyName("created_rows")]
    public int CreatedRows { get; init; }

    [JsonPropertyName("updated_rows")]
    public int UpdatedRows { get; init; }

    [JsonPropertyName("skipped_rows")]
    public int SkippedRows { get; init; }

    [JsonPropertyName("error_rows")]
    public int ErrorRows { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyCollection<ImportPatientRowErrorResponse> Errors { get; init; } = [];
}
