using UploadService.Core.Entities;
using UploadService.Core.UseCases.Videos;

namespace UploadService.API.Presenters;

public record VideoPresenter(
    Guid Id,
    Guid UserId,
    string OriginalFileName,
    string FileName,
    long FileSizeBytes,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public static class VideoPresenterMapper
{
    public static VideoPresenter ToPresenter(VideoOutput o) =>
        new(o.Id, o.UserId, o.OriginalFileName, o.FileName,
            o.FileSizeBytes, o.Status.ToString(), o.ErrorMessage,
            o.CreatedAt, o.UpdatedAt);
}
