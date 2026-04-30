using MedicalCenter.Api.Extensions;
using MedicalCenter.Application.Features.Imports;
using MedicalCenter.Contracts.Imports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalCenter.Api.Controllers.V1;

[ApiController]
[Route("api/v1/importaciones")]
[Authorize(Policy = "PatientsManage")]
public sealed class ImportacionesController(IImportPatientsOrchestrator orchestrator) : ControllerBase
{
    [HttpPost("pacientes/upload-url")]
    public async Task<IActionResult> CreateUploadUrl(
        [FromBody] CreateImportUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await orchestrator.CreateUploadUrlAsync(
            request.FileName,
            request.SizeBytes,
            request.ContentType,
            userId,
            cancellationToken);

        return Ok(new CreateImportUploadUrlResponse
        {
            ImportacionId = result.ImportacionId,
            UploadUrl = result.UploadUrl,
            StorageKey = result.StorageKey,
            ExpiresAt = result.ExpiresAt,
            RequiredHeaders = result.RequiredHeaders
        });
    }

    [HttpPost("pacientes/{id:guid}/confirmar")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await orchestrator.ConfirmAsync(id, userId, cancellationToken);

        return Ok(new ConfirmImportResponse
        {
            ImportacionId = result.ImportacionId,
            Estado = result.Estado,
            TotalRows = result.Result.TotalRows,
            CreatedRows = result.Result.CreatedRows,
            UpdatedRows = result.Result.UpdatedRows,
            SkippedRows = result.Result.SkippedRows,
            ErrorRows = result.Result.ErrorRows,
            Errors = result.Result.Errors.Select(e => new ImportPatientRowErrorResponse
            {
                RowNumber = e.RowNumber,
                Message = e.Message
            }).ToArray()
        });
    }

    [HttpGet("pacientes/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await orchestrator.GetAsync(id, userId, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new ImportSummaryResponse
        {
            ImportacionId = result.ImportacionId,
            Tipo = result.Tipo,
            Estado = result.Estado,
            FileName = result.FileName,
            SizeBytes = result.SizeBytes,
            TotalFilas = result.TotalFilas,
            FilasInsertadas = result.FilasInsertadas,
            FilasActualizadas = result.FilasActualizadas,
            FilasConError = result.FilasConError,
            ErrorMessage = result.ErrorMessage,
            CreatedAt = result.CreatedAt,
            StartedAt = result.StartedAt,
            FinishedAt = result.FinishedAt,
            Errors = result.Errors.Select(e => new ImportPatientRowErrorResponse
            {
                RowNumber = e.RowNumber,
                Message = e.Message
            }).ToArray()
        });
    }

    // Legacy: direct multipart upload — useful for local testing and CLI scripts
    [HttpPost("pacientes")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportDirect(IFormFile file, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await using var stream = file.OpenReadStream();
        var result = await orchestrator.DirectImportAsync(
            file.FileName,
            file.Length,
            file.ContentType,
            stream,
            userId,
            cancellationToken);

        return Ok(new ImportPatientsResponse
        {
            TotalRows = result.Result.TotalRows,
            CreatedRows = result.Result.CreatedRows,
            UpdatedRows = result.Result.UpdatedRows,
            SkippedRows = result.Result.SkippedRows,
            ErrorRows = result.Result.ErrorRows,
            Errors = result.Result.Errors.Select(e => new ImportPatientRowErrorResponse
            {
                RowNumber = e.RowNumber,
                Message = e.Message
            }).ToArray()
        });
    }
}
