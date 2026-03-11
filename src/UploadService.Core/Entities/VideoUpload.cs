namespace UploadService.Core.Entities;

public class VideoUpload
{
    private VideoUpload() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public VideoUploadStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static VideoUpload Create(
        Guid userId,
        string originalFileName,
        string contentType,
        long fileSizeBytes) =>
        new()
        {
            Id               = Guid.NewGuid(),
            UserId           = userId,
            FileName         = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}",
            OriginalFileName = originalFileName,
            ContentType      = contentType,
            FileSizeBytes    = fileSizeBytes,
            Status           = VideoUploadStatus.Pending,
            CreatedAt        = DateTime.UtcNow
        };

    public void SetStoragePath(string path)
    {
        StoragePath = path;
        Status      = VideoUploadStatus.Uploaded;
        UpdatedAt   = DateTime.UtcNow;
    }

    public void MarkAsQueued()
    {
        Status    = VideoUploadStatus.Queued;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string reason)
    {
        Status       = VideoUploadStatus.Failed;
        ErrorMessage = reason;
        UpdatedAt    = DateTime.UtcNow;
    }
}
