namespace UploadService.Domain.Entities;

public sealed class VideoUpload
{
    public Guid     Id               { get; private set; }
    public Guid     UserId           { get; private set; }
    public string   OriginalFileName { get; private set; } = string.Empty;
    public string   StoredFileName   { get; private set; } = string.Empty;
    public string   ContentType      { get; private set; } = string.Empty;
    public long     FileSizeBytes    { get; private set; }
    public string   StoragePath      { get; private set; } = string.Empty;
    public UploadStatus Status       { get; private set; }
    public string?  ErrorMessage     { get; private set; }
    public DateTime CreatedAt        { get; private set; }
    public DateTime UpdatedAt        { get; private set; }

    private VideoUpload() { }

    public static VideoUpload Create(
        Guid   userId,
        string originalFileName,
        string contentType,
        long   fileSizeBytes)
    {
        return new VideoUpload
        {
            Id               = Guid.NewGuid(),
            UserId           = userId,
            OriginalFileName = originalFileName,
            StoredFileName   = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}",
            ContentType      = contentType,
            FileSizeBytes    = fileSizeBytes,
            Status           = UploadStatus.Pending,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };
    }

    public void MarkUploaded(string storagePath)
    {
        StoragePath = storagePath;
        Status      = UploadStatus.Uploaded;
        UpdatedAt   = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status       = UploadStatus.Failed;
        ErrorMessage = reason;
        UpdatedAt    = DateTime.UtcNow;
    }
}
