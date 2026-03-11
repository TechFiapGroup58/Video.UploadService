using UploadService.Core.Entities;

namespace UploadService.Core.UseCases.Videos;

public record VideoOutput(
    Guid Id,
    Guid UserId,
    string OriginalFileName,
    string FileName,
    long FileSizeBytes,
    VideoUploadStatus Status,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
