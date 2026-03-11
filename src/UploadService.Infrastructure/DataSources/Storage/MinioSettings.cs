namespace UploadService.Infrastructure.DataSources.Storage;

public sealed class MinioSettings
{
    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = "videos";
    public bool UseSSL { get; init; } = false;
}
