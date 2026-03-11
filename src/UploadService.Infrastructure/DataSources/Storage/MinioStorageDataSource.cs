using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using UploadService.Core.Exceptions;
using UploadService.Core.Gateways;

namespace UploadService.Infrastructure.DataSources.Storage;

public sealed class MinioStorageDataSource(IMinioClient minio, IOptions<MinioSettings> options) : IStorageGateway
{
    private readonly MinioSettings _settings = options.Value;

    public async Task<string> UploadAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);

        var args = new PutObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(fileName)
            .WithStreamData(content)
            .WithObjectSize(content.Length < 0 ? -1 : content.Length)
            .WithContentType(contentType);

        try
        {
            await minio.PutObjectAsync(args, ct);
            return $"{_settings.BucketName}/{fileName}";
        }
        catch (Exception ex)
        {
            throw new StorageException($"Falha ao fazer upload para o MinIO: {ex.Message}");
        }
    }

    public async Task<string> GetPresignedUrlAsync(string storagePath, TimeSpan expiry, CancellationToken ct = default)
    {
        var objectName = ExtractObjectName(storagePath);

        var args = new PresignedGetObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectName)
            .WithExpiry((int)expiry.TotalSeconds);

        return await minio.PresignedGetObjectAsync(args);
    }

    public async Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var objectName = ExtractObjectName(storagePath);

        var args = new RemoveObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectName);

        await minio.RemoveObjectAsync(args, ct);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var exists = await minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_settings.BucketName), ct);

        if (!exists)
            await minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_settings.BucketName), ct);
    }

    private static string ExtractObjectName(string storagePath)
    {
        var slash = storagePath.IndexOf('/');
        return slash >= 0 ? storagePath[(slash + 1)..] : storagePath;
    }
}
