using UploadService.Domain.Entities;

namespace UploadService.Application.DTOs;

public sealed record UploadVideoResponse(
    Guid        Id,
    string      OriginalFileName,
    string      StoredFileName,
    long        FileSizeBytes,
    UploadStatus Status,
    string      StoragePath,
    DateTime    CreatedAt
);

public sealed record VideoUploadSummary(
    Guid        Id,
    string      OriginalFileName,
    long        FileSizeBytes,
    UploadStatus Status,
    string?     ErrorMessage,
    DateTime    CreatedAt
);
