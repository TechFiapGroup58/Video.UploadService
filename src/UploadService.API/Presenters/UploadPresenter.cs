using UploadService.Core.Entities;
using UploadService.Core.UseCases.Uploads;

namespace UploadService.API.Presenters;

public record UploadPresenter(
    Guid Id,
    Guid UserId,
    string OriginalFileName,
    string FileName,
    long FileSizeBytes,
    string Status,
    DateTime CreatedAt
);

public static class UploadPresenterMapper
{
    public static UploadPresenter ToPresenter(UploadVideoOutput o) =>
        new(o.Id, o.UserId, o.OriginalFileName, o.FileName,
            o.FileSizeBytes, o.Status.ToString(), o.CreatedAt);
}
