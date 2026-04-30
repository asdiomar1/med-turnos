namespace MedicalCenter.Application.Abstractions.Storage;

public sealed record PresignedUpload(
    string Url,
    IReadOnlyDictionary<string, string> RequiredHeaders,
    DateTimeOffset ExpiresAt);
