using MedicalCenter.Domain.Common;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.Domain.Entities;

public sealed class Importacion : Entity<Guid>
{
    private Importacion() { }

    public Importacion(
        Guid id,
        string tipo,
        Guid usuarioId,
        string fileName,
        ImportacionStorageInfo storageInfo,
        DateTimeOffset expiresAt)
    {
        Id = id;
        Tipo = tipo;
        Estado = ImportacionEstado.PendienteSubida;
        UsuarioId = usuarioId;
        FileName = fileName;
        StorageProvider = storageInfo.Provider;
        StorageBucket = storageInfo.Bucket;
        StorageKey = storageInfo.Key;
        SizeBytes = storageInfo.SizeBytes;
        ContentType = storageInfo.ContentType;
        Sha256 = null;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Tipo { get; private set; } = string.Empty;
    public string Estado { get; private set; } = ImportacionEstado.PendienteSubida;
    public Guid UsuarioId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StorageProvider { get; private set; } = string.Empty;
    public string StorageBucket { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public string? Sha256 { get; private set; }
    public int TotalFilas { get; private set; }
    public int FilasValidas { get; private set; }
    public int FilasConError { get; private set; }
    public int FilasInsertadas { get; private set; }
    public int FilasActualizadas { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    public void MarcarSubido()
    {
        Estado = ImportacionEstado.Subido;
    }

    public void MarcarProcesando()
    {
        Estado = ImportacionEstado.Procesando;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void MarcarProcesado(int totalFilas, int filasValidas, int filasConError, int filasInsertadas, int filasActualizadas)
    {
        Estado = ImportacionEstado.Procesado;
        TotalFilas = totalFilas;
        FilasValidas = filasValidas;
        FilasConError = filasConError;
        FilasInsertadas = filasInsertadas;
        FilasActualizadas = filasActualizadas;
        FinishedAt = DateTimeOffset.UtcNow;
    }

    public void MarcarFallido(string errorMessage)
    {
        Estado = ImportacionEstado.Fallido;
        ErrorMessage = errorMessage;
        FinishedAt = DateTimeOffset.UtcNow;
    }

    public bool PuedeConfirmar() =>
        Estado == ImportacionEstado.PendienteSubida || Estado == ImportacionEstado.Subido;

    public bool PerteneceA(Guid usuarioId) => UsuarioId == usuarioId;
}

public sealed class ImportacionStorageInfo(
    string provider,
    string bucket,
    string key,
    long sizeBytes,
    string contentType)
{
    public string Provider { get; } = provider;
    public string Bucket { get; } = bucket;
    public string Key { get; } = key;
    public long SizeBytes { get; } = sizeBytes;
    public string ContentType { get; } = contentType;
}
