using UploadService.Application.DTOs;
using UploadService.Domain.Entities;

namespace UploadService.Application.Interfaces;

public interface IUploadService
{
    Task<UploadVideoResponse>         UploadAsync(Guid userId, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken ct = default);
    Task<VideoUploadSummary>          GetByIdAsync(Guid uploadId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<VideoUploadSummary>> ListByUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IVideoUploadRepository
{
    Task<VideoUpload?>              GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<VideoUpload>>  GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task                            AddAsync(VideoUpload upload, CancellationToken ct = default);
    Task                            UpdateAsync(VideoUpload upload, CancellationToken ct = default);
}

public interface IStorageService
{
    Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken ct = default);
    Task         DeleteAsync(string storagePath, CancellationToken ct = default);
}

public interface IMessagePublisher
{
    Task PublishVideoUploadedAsync(Guid uploadId, Guid userId, string storagePath, CancellationToken ct = default);
}

public interface IVideoValidator
{
    void Validate(string contentType, long fileSizeBytes);
}
