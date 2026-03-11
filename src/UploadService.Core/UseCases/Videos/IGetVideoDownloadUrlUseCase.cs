namespace UploadService.Core.UseCases.Videos;

public interface IGetVideoDownloadUrlUseCase
{
    Task<string> ExecuteAsync(Guid uploadId, Guid requestingUserId, CancellationToken ct = default);
}
