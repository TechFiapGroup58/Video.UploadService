using UploadService.Core.Exceptions;
using UploadService.Core.Gateways;

namespace UploadService.Core.UseCases.Videos;

public sealed class GetVideoUseCase(IVideoUploadGateway uploadGateway) : IGetVideoUseCase
{
    public async Task<VideoOutput> ExecuteAsync(Guid uploadId, Guid requestingUserId, CancellationToken ct = default)
    {
        var upload = await uploadGateway.FindByIdAsync(uploadId, ct)
            ?? throw new VideoUploadNotFoundException(uploadId);

        if (upload.UserId != requestingUserId)
            throw new UnauthorizedUploadException();

        return new VideoOutput(
            upload.Id, upload.UserId, upload.OriginalFileName, upload.FileName,
            upload.FileSizeBytes, upload.Status, upload.ErrorMessage,
            upload.CreatedAt, upload.UpdatedAt);
    }
}
