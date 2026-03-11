namespace UploadService.Infrastructure.DataSources.Messaging;

public sealed record VideoUploadedMessage(
    Guid UploadId,
    Guid UserId,
    string StoragePath,
    string OriginalFileName,
    DateTime OccurredAt
);
