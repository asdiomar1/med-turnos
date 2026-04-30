using MedicalCenter.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace MedicalCenter.Infrastructure.Options;

public sealed class ImportsOptions : IImportsOptions
{
    public const string SectionName = "Imports";

    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10 MB
    public int PresignTtlSeconds { get; init; } = 300;
    public string StorageBucket { get; init; } = "medicalcenter-imports-dev";
    public string StorageProvider { get; init; } = "r2";
}

public sealed class ImportsOptionsAdapter(IOptions<ImportsOptions> inner) : IImportsOptions
{
    public long MaxFileSizeBytes => inner.Value.MaxFileSizeBytes;
    public int PresignTtlSeconds => inner.Value.PresignTtlSeconds;
    public string StorageBucket => inner.Value.StorageBucket;
    public string StorageProvider => inner.Value.StorageProvider;
}
