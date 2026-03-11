namespace UploadService.Core.UseCases.Uploads;

public record UploadVideoInput(
    Guid UserId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content
);
