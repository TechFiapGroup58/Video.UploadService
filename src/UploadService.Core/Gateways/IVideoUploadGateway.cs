using UploadService.Core.Entities;

namespace UploadService.Core.Gateways;

public interface IVideoUploadGateway
{
    Task<VideoUpload?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<VideoUpload>> FindByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task SaveAsync(VideoUpload upload, CancellationToken ct = default);
    Task UpdateAsync(VideoUpload upload, CancellationToken ct = default);
}
