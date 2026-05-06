using System.Text.RegularExpressions;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.Abstractions.Persistence;
using MedicalCenter.Application.Abstractions.Storage;
using MedicalCenter.Application.DTOs;
using MedicalCenter.Application.Exceptions;
using MedicalCenter.Application.Features.AdminEventFeed;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Application.Features.Imports;

public sealed partial class ImportPatientsOrchestrator(
    IObjectStorage objectStorage,
    IImportsOptions importsOptions,
    IImportacionesRepository importacionesRepository,
    IAdminEventFeedRepository adminEventFeedRepository,
    IImportPatientsService importPatientsService,
    IUnitOfWork unitOfWork) : IImportPatientsOrchestrator
{
    private static readonly string[] AllowedContentTypes =
    [
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel"
    ];

    public async Task<CreateImportUploadUrlDto> CreateUploadUrlAsync(
        string fileName,
        long sizeBytes,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken)
    {
        ValidateUploadRequest(fileName, sizeBytes, contentType, importsOptions.MaxFileSizeBytes);

        var importacionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var storageKey = BuildStorageKey(importacionId, fileName, now);
        var ttl = TimeSpan.FromSeconds(importsOptions.PresignTtlSeconds);

        var presigned = await objectStorage.CreatePresignedPutAsync(
            storageKey,
            contentType,
            sizeBytes,
            ttl,
            cancellationToken);

        var sanitizedName = SanitizeFileName(fileName);
        var storageInfo = new ImportacionStorageInfo(
            importsOptions.StorageProvider,
            importsOptions.StorageBucket,
            storageKey,
            sizeBytes,
            contentType);
        var importacion = new Importacion(
            importacionId,
            tipo: "pacientes",
            usuarioId: userId,
            fileName: sanitizedName,
            storageInfo: storageInfo,
            expiresAt: presigned.ExpiresAt);

        await importacionesRepository.AddAsync(importacion, cancellationToken);
        await AddAuditEventAsync(
            userId, importacionId,
            AdminEventFeedConstants.ActionCodes.ImportacionCreada,
            "Importación de pacientes iniciada",
            $"Archivo '{sanitizedName}' ({sizeBytes / 1024} KB) registrado y URL de subida generada.",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateImportUploadUrlDto(
            importacionId,
            presigned.Url,
            storageKey,
            presigned.ExpiresAt,
            presigned.RequiredHeaders);
    }

    public async Task<ConfirmImportDto> ConfirmAsync(
        Guid importacionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var importacion = await importacionesRepository.GetByIdAsync(importacionId, cancellationToken)
            ?? throw new NotFoundException("Importación no encontrada.");

        if (!importacion.PerteneceA(userId))
        {
            throw new ForbiddenException("No tiene permiso para confirmar esta importación.");
        }

        if (!importacion.PuedeConfirmar())
        {
            throw new ConflictException(
                $"La importación está en estado '{importacion.Estado}' y no puede confirmarse.",
                "estado_invalido");
        }

        var exists = await objectStorage.ExistsAsync(importacion.StorageKey, cancellationToken);
        if (!exists)
        {
            importacion.MarcarFallido("El archivo no fue encontrado en el storage.");
            await AddAuditEventAsync(
                userId, importacionId,
                AdminEventFeedConstants.ActionCodes.ImportacionFallida,
                "Importación de pacientes fallida",
                "El archivo no fue encontrado en el storage al intentar confirmar.",
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ConflictException("El archivo aún no fue subido al storage.", "archivo_no_subido");
        }

        importacion.MarcarProcesando();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ImportPatientsResultDto result;
        try
        {
            await using var stream = await objectStorage.OpenReadAsync(importacion.StorageKey, cancellationToken);
            ValidateMagicBytes(stream);
            result = await importPatientsService.ImportAsync(stream, cancellationToken);
        }
        catch (Exception)
        {
            importacion.MarcarFallido("Error al procesar el archivo XLSX.");
            await AddAuditEventAsync(
                userId, importacionId,
                AdminEventFeedConstants.ActionCodes.ImportacionFallida,
                "Importación de pacientes fallida",
                $"Error al procesar el archivo '{importacion.FileName}'.",
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        importacion.MarcarProcesado(
            result.TotalRows,
            result.TotalRows - result.ErrorRows,
            result.ErrorRows,
            result.CreatedRows,
            result.UpdatedRows);

        if (result.Errors.Count > 0)
        {
            var errors = result.Errors.Select(e => new ImportacionError(importacionId, e.RowNumber, e.Message));
            await importacionesRepository.AddErrorsAsync(errors, cancellationToken);
        }

        await AddAuditEventAsync(
            userId, importacionId,
            AdminEventFeedConstants.ActionCodes.ImportacionConfirmada,
            "Importación de pacientes completada",
            $"'{importacion.FileName}': {result.TotalRows} filas — {result.CreatedRows} creados, {result.UpdatedRows} actualizados, {result.ErrorRows} errores.",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ConfirmImportDto(importacionId, importacion.Estado, result);
    }

    public async Task<ConfirmImportDto> DirectImportAsync(
        string fileName,
        long sizeBytes,
        string contentType,
        Stream xlsxStream,
        Guid userId,
        CancellationToken cancellationToken)
    {
        ValidateUploadRequest(fileName, sizeBytes, contentType, importsOptions.MaxFileSizeBytes);
        ValidateMagicBytes(xlsxStream);

        var importacionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var storageKey = BuildStorageKey(importacionId, fileName, now);
        var sanitizedName = SanitizeFileName(fileName);
        var storageInfo = new ImportacionStorageInfo(
            importsOptions.StorageProvider,
            importsOptions.StorageBucket,
            storageKey,
            sizeBytes,
            contentType);

        var importacion = new Importacion(
            importacionId,
            tipo: "pacientes",
            usuarioId: userId,
            fileName: sanitizedName,
            storageInfo: storageInfo,
            expiresAt: now);

        await importacionesRepository.AddAsync(importacion, cancellationToken);
        await AddAuditEventAsync(
            userId, importacionId,
            AdminEventFeedConstants.ActionCodes.ImportacionCreada,
            "Importación de pacientes iniciada (carga directa)",
            $"Archivo '{sanitizedName}' ({sizeBytes / 1024} KB) recibido vía carga directa.",
            cancellationToken);

        await objectStorage.UploadAsync(storageKey, xlsxStream, contentType, cancellationToken);
        importacion.MarcarSubido();

        importacion.MarcarProcesando();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        ImportPatientsResultDto result;
        try
        {
            xlsxStream.Position = 0;
            result = await importPatientsService.ImportAsync(xlsxStream, cancellationToken);
        }
        catch (Exception)
        {
            importacion.MarcarFallido("Error al procesar el archivo XLSX.");
            await AddAuditEventAsync(
                userId, importacionId,
                AdminEventFeedConstants.ActionCodes.ImportacionFallida,
                "Importación de pacientes fallida",
                $"Error al procesar el archivo '{sanitizedName}'.",
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        importacion.MarcarProcesado(
            result.TotalRows,
            result.TotalRows - result.ErrorRows,
            result.ErrorRows,
            result.CreatedRows,
            result.UpdatedRows);

        if (result.Errors.Count > 0)
        {
            var errors = result.Errors.Select(e => new ImportacionError(importacionId, e.RowNumber, e.Message));
            await importacionesRepository.AddErrorsAsync(errors, cancellationToken);
        }

        await AddAuditEventAsync(
            userId, importacionId,
            AdminEventFeedConstants.ActionCodes.ImportacionConfirmada,
            "Importación de pacientes completada",
            $"'{sanitizedName}': {result.TotalRows} filas — {result.CreatedRows} creados, {result.UpdatedRows} actualizados, {result.ErrorRows} errores.",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ConfirmImportDto(importacionId, importacion.Estado, result);
    }

    public async Task<ImportSummaryDto?> GetAsync(Guid importacionId, Guid userId, CancellationToken cancellationToken)
    {
        var importacion = await importacionesRepository.GetByIdAsync(importacionId, cancellationToken);
        if (importacion is null || !importacion.PerteneceA(userId))
        {
            return null;
        }

        var errors = importacion.Estado is ImportacionEstado.Procesado or ImportacionEstado.Fallido
            ? await importacionesRepository.GetErrorsAsync(importacionId, cancellationToken)
            : (IReadOnlyCollection<ImportacionError>)[];

        return new ImportSummaryDto(
            importacion.Id,
            importacion.Tipo,
            importacion.Estado,
            importacion.FileName,
            importacion.SizeBytes,
            importacion.TotalFilas,
            importacion.FilasInsertadas,
            importacion.FilasActualizadas,
            importacion.FilasConError,
            importacion.ErrorMessage,
            importacion.CreatedAt,
            importacion.StartedAt,
            importacion.FinishedAt,
            errors.Select(e => new ImportPatientRowErrorDto(e.RowNumber, e.Message)).ToArray());
    }

    private Task AddAuditEventAsync(
        Guid actorUserId,
        Guid importacionId,
        string actionCode,
        string title,
        string summary,
        CancellationToken cancellationToken)
    {
        var entry = new AdminEventFeedEntry(new AdminEventFeedEntryCreateParams(
            0,
            DateTimeOffset.UtcNow,
            actorUserId,
            AdminEventFeedConstants.DefaultActorLabel,
            actionCode,
            AdminEventFeedConstants.ActionFamilyImport,
            AdminEventFeedConstants.EntityTypes.Importacion,
            importacionId.ToString(),
            null,
            null,
            null,
            null,
            null,
            title,
            summary,
            AdminEventFeedConstants.SourceSystemApi,
            $"importacion:{actionCode}:{importacionId:N}:{Guid.NewGuid():N}",
            "{}"));

        return adminEventFeedRepository.AddAsync(entry, cancellationToken);
    }

    private static void ValidateUploadRequest(string fileName, long sizeBytes, string contentType, long maxSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("El nombre de archivo es obligatorio.", null);
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not ".xlsx" and not ".xls")
        {
            throw new ValidationException("El archivo debe tener extensión .xlsx o .xls.", null);
        }

        if (!AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("El tipo de contenido del archivo no es válido. Solo se admiten archivos Excel (.xlsx, .xls).", null);
        }

        if (sizeBytes <= 0)
        {
            throw new ValidationException("El tamaño del archivo debe ser mayor a 0.", null);
        }

        if (sizeBytes > maxSizeBytes)
        {
            throw new ValidationException($"El archivo supera el tamaño máximo permitido de {maxSizeBytes / 1024 / 1024} MB.", null);
        }
    }

    private static void ValidateMagicBytes(Stream stream)
    {
        // XLSX files are ZIP archives — magic bytes: PK\x03\x04
        Span<byte> header = stackalloc byte[4];
        var read = stream.Read(header);
        stream.Position = 0;

        if (read < 4 || header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
        {
            throw new ValidationException("El archivo no es un archivo Excel válido.", null);
        }
    }

    private static string BuildStorageKey(Guid importacionId, string fileName, DateTimeOffset now) =>
        $"imports/pacientes/{now:yyyy}/{now:MM}/{importacionId}/{SanitizeFileName(fileName)}";

    [GeneratedRegex(@"[^a-z0-9._-]")]
    private static partial Regex SanitizeFileNameRegex();

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var sanitized = SanitizeFileNameRegex().Replace(name.ToLowerInvariant(), "_");
        if (sanitized.Length > 80)
        {
            sanitized = sanitized[..80];
        }

        return $"{sanitized}{ext}";
    }
}
