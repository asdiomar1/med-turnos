using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Imports;

public interface IImportPatientsOrchestrator
{
    Task<CreateImportUploadUrlDto> CreateUploadUrlAsync(string fileName, long sizeBytes, string contentType, Guid userId, CancellationToken cancellationToken);
    Task<ConfirmImportDto> ConfirmAsync(Guid importacionId, Guid userId, CancellationToken cancellationToken);
    Task<ImportSummaryDto?> GetAsync(Guid importacionId, Guid userId, CancellationToken cancellationToken);

    /// <summary>Convenience path for direct multipart upload (CLI/admin). Uploads stream to R2, then confirms.</summary>
    Task<ConfirmImportDto> DirectImportAsync(string fileName, long sizeBytes, string contentType, Stream xlsxStream, Guid userId, CancellationToken cancellationToken);
}
