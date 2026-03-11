using UploadService.Core.Entities;
using UploadService.Core.Exceptions;
using UploadService.Core.Gateways;

namespace UploadService.Core.UseCases.Videos;

public sealed class GetVideoDownloadUrlUseCase(
    IVideoUploadGateway uploadGateway,
    IStorageGateway storageGateway) : IGetVideoDownloadUrlUseCase
{
    public async Task<string> ExecuteAsync(Guid uploadId, Guid requestingUserId, CancellationToken ct = default)
    {
        var upload = await uploadGateway.FindByIdAsync(uploadId, ct)
            ?? throw new VideoUploadNotFoundException(uploadId);

        if (upload.UserId != requestingUserId)
            throw new UnauthorizedUploadException();

        if (upload.Status != VideoUploadStatus.Uploaded && upload.Status != VideoUploadStatus.Queued)
            throw new InvalidVideoException("vídeo ainda não disponível para download.");

        return await storageGateway.GetPresignedUrlAsync(upload.StoragePath, TimeSpan.FromMinutes(15), ct);
    }
}
