namespace MedicalCenter.Application.Abstractions.Storage;

public interface IImportsOptions
{
    long MaxFileSizeBytes { get; }
    int PresignTtlSeconds { get; }
    string StorageBucket { get; }
    string StorageProvider { get; }
}
