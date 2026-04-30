using Amazon.S3;
using Amazon.S3.Model;
using MedicalCenter.Application.Abstractions.Storage;
using MedicalCenter.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MedicalCenter.Infrastructure.Storage;

public sealed class R2ObjectStorage(IAmazonS3 s3, IOptions<R2Options> options) : IObjectStorage
{
    private string Bucket => options.Value.Bucket;

    public Task<PresignedUpload> CreatePresignedPutAsync(
        string key,
        string contentType,
        long maxSizeBytes,
        TimeSpan ttl,
        CancellationToken ct)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(ttl),
            ContentType = contentType,
        };

        var url = s3.GetPreSignedURL(request);

        var requiredHeaders = new Dictionary<string, string>
        {
            ["Content-Type"] = contentType
        };

        var result = new PresignedUpload(url, requiredHeaders, DateTimeOffset.UtcNow.Add(ttl));
        return Task.FromResult(result);
    }

    public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct)
    {
        var request = new PutObjectRequest
        {
            BucketName = Bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false,
        };
        await s3.PutObjectAsync(request, ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct)
    {
        try
        {
            await s3.GetObjectMetadataAsync(Bucket, key, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<long> GetSizeAsync(string key, CancellationToken ct)
    {
        var meta = await s3.GetObjectMetadataAsync(Bucket, key, ct);
        return meta.ContentLength;
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken ct)
    {
        var response = await s3.GetObjectAsync(Bucket, key, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        await s3.DeleteObjectAsync(Bucket, key, ct);
    }
}
