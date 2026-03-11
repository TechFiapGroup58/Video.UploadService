namespace UploadService.Core.UseCases.Uploads;

public interface IUploadVideoUseCase
{
    Task<UploadVideoOutput> ExecuteAsync(UploadVideoInput input, CancellationToken ct = default);
}
