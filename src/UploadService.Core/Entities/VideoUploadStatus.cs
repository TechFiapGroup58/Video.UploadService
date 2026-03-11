namespace UploadService.Core.Entities;

public enum VideoUploadStatus
{
    Pending  = 0,
    Uploaded = 1,
    Queued   = 2,
    Failed   = 3
}
