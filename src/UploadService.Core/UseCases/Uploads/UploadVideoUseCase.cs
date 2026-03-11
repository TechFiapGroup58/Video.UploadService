using UploadService.Core.Entities;
using UploadService.Core.Gateways;

namespace UploadService.Core.UseCases.Uploads;

public sealed class UploadVideoUseCase(
    IVideoUploadGateway uploadGateway,
    IStorageGateway storageGateway,
    IVideoProcessingGateway processingGateway,
    IVideoValidatorGateway validator) : IUploadVideoUseCase
{
    public async Task<UploadVideoOutput> ExecuteAsync(UploadVideoInput input, CancellationToken ct = default)
    {
        validator.Validate(input.ContentType, input.FileSizeBytes);

        var upload = VideoUpload.Create(
            input.UserId,
            input.OriginalFileName,
            input.ContentType,
            input.FileSizeBytes);

        await uploadGateway.SaveAsync(upload, ct);

        try
        {
            var storagePath = await storageGateway.UploadAsync(
                upload.FileName,
                input.ContentType,
                input.Content,
                ct);

            upload.SetStoragePath(storagePath);
            await uploadGateway.UpdateAsync(upload, ct);

            await processingGateway.PublishVideoUploadedAsync(
                upload.Id,
                upload.UserId,
                storagePath,
                upload.OriginalFileName,
                ct);

            upload.MarkAsQueued();
            await uploadGateway.UpdateAsync(upload, ct);
        }
        catch (Exception ex)
        {
            upload.MarkAsFailed(ex.Message);
            await uploadGateway.UpdateAsync(upload, ct);
            throw;
        }

        return ToOutput(upload);
    }

    private static UploadVideoOutput ToOutput(VideoUpload u) =>
        new(u.Id, u.UserId, u.OriginalFileName, u.FileName, u.FileSizeBytes, u.Status, u.CreatedAt);
}
