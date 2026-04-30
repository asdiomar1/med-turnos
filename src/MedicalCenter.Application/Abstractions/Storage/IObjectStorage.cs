namespace MedicalCenter.Application.Abstractions.Storage;

public interface IObjectStorage
{
    Task<PresignedUpload> CreatePresignedPutAsync(string key, string contentType, long maxSizeBytes, TimeSpan ttl, CancellationToken ct);
    Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct);
    Task<bool> ExistsAsync(string key, CancellationToken ct);
    Task<long> GetSizeAsync(string key, CancellationToken ct);
    Task<Stream> OpenReadAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}
