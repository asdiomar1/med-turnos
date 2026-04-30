using MedicalCenter.Application.DTOs;

namespace MedicalCenter.Application.Features.Imports;

public interface IImportPatientsService
{
    Task<ImportPatientsResultDto> ImportAsync(Stream xlsxStream, CancellationToken cancellationToken);
}
