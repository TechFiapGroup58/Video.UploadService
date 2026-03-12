using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using UploadService.Application.Interfaces;

namespace UploadService.Infrastructure.Storage;

public sealed class MinioSettings
{
    public string Endpoint   { get; init; } = string.Empty;
    public string AccessKey  { get; init; } = string.Empty;
    public string SecretKey  { get; init; } = string.Empty;
    public string BucketName { get; init; } = "videos";
    public bool   UseSSL     { get; init; } = false;
}

public sealed class MinioStorageService : IStorageService
{
    private readonly IAmazonS3   _s3;
    private readonly MinioSettings _settings;

    public MinioStorageService(IAmazonS3 s3, IOptions<MinioSettings> options)
    {
        _s3       = s3;
        _settings = options.Value;
    }

    public async Task<string> UploadAsync(
        string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName  = _settings.BucketName,
            Key         = fileName,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await _s3.PutObjectAsync(request, ct);
        return $"{_settings.BucketName}/{fileName}";
    }

    public async Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var key = storagePath.Contains('/') ? storagePath[(storagePath.IndexOf('/') + 1)..] : storagePath;
        await _s3.DeleteObjectAsync(_settings.BucketName, key, ct);
    }
}
