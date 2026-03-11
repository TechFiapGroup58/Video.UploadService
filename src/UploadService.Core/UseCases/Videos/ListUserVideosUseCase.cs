using UploadService.Core.Gateways;

namespace UploadService.Core.UseCases.Videos;

public sealed class ListUserVideosUseCase(IVideoUploadGateway uploadGateway) : IListUserVideosUseCase
{
    public async Task<IReadOnlyList<VideoOutput>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var uploads = await uploadGateway.FindByUserIdAsync(userId, ct);

        return uploads.Select(u => new VideoOutput(
            u.Id, u.UserId, u.OriginalFileName, u.FileName,
            u.FileSizeBytes, u.Status, u.ErrorMessage,
            u.CreatedAt, u.UpdatedAt)).ToList();
    }
}
