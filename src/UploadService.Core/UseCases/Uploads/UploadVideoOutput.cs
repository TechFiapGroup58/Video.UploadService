using UploadService.Core.Entities;

namespace UploadService.Core.UseCases.Uploads;

public record UploadVideoOutput(
    Guid Id,
    Guid UserId,
    string OriginalFileName,
    string FileName,
    long FileSizeBytes,
    VideoUploadStatus Status,
    DateTime CreatedAt
);
