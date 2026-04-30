namespace MedicalCenter.Application.DTOs;

public sealed record ImportPatientRowErrorDto(int RowNumber, string Message);

public sealed record ImportPatientsResultDto(
    int TotalRows,
    int CreatedRows,
    int UpdatedRows,
    int SkippedRows,
    int ErrorRows,
    IReadOnlyCollection<ImportPatientRowErrorDto> Errors);

public sealed record CreateImportUploadUrlDto(
    Guid ImportacionId,
    string UploadUrl,
    string StorageKey,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string> RequiredHeaders);

public sealed record ConfirmImportDto(
    Guid ImportacionId,
    string Estado,
    ImportPatientsResultDto Result);

public sealed record ImportSummaryDto(
    Guid ImportacionId,
    string Tipo,
    string Estado,
    string FileName,
    long SizeBytes,
    int TotalFilas,
    int FilasInsertadas,
    int FilasActualizadas,
    int FilasConError,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    IReadOnlyCollection<ImportPatientRowErrorDto> Errors);
