namespace UploadService.Core.UseCases.Videos;

public interface IListUserVideosUseCase
{
    Task<IReadOnlyList<VideoOutput>> ExecuteAsync(Guid userId, CancellationToken ct = default);
}
