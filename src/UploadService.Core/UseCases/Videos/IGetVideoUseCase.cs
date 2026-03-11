namespace UploadService.Core.UseCases.Videos;

public interface IGetVideoUseCase
{
    Task<VideoOutput> ExecuteAsync(Guid uploadId, Guid requestingUserId, CancellationToken ct = default);
}
