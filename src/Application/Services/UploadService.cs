using UploadService.Application.DTOs;
using UploadService.Application.Interfaces;
using UploadService.Domain.Entities;
using UploadService.Domain.Exceptions;

namespace UploadService.Application.Services;

public sealed class UploadService : IUploadService
{
    private readonly IVideoUploadRepository _repository;
    private readonly IStorageService        _storage;
    private readonly IMessagePublisher      _publisher;
    private readonly IVideoValidator        _validator;

    public UploadService(
        IVideoUploadRepository repository,
        IStorageService        storage,
        IMessagePublisher      publisher,
        IVideoValidator        validator)
    {
        _repository = repository;
        _storage    = storage;
        _publisher  = publisher;
        _validator  = validator;
    }

    public async Task<UploadVideoResponse> UploadAsync(
        Guid   userId,
        string fileName,
        string contentType,
        long   sizeBytes,
        Stream content,
        CancellationToken ct = default)
    {
        _validator.Validate(contentType, sizeBytes);

        var upload = VideoUpload.Create(userId, fileName, contentType, sizeBytes);
        await _repository.AddAsync(upload, ct);

        try
        {
            var storagePath = await _storage.UploadAsync(upload.StoredFileName, contentType, content, ct);
            upload.MarkUploaded(storagePath);
            await _repository.UpdateAsync(upload, ct);

            await _publisher.PublishVideoUploadedAsync(upload.Id, userId, storagePath, ct);
        }
        catch (Exception ex)
        {
            upload.MarkFailed(ex.Message);
            await _repository.UpdateAsync(upload, ct);
            throw;
        }

        return ToResponse(upload);
    }

    public async Task<VideoUploadSummary> GetByIdAsync(Guid uploadId, Guid userId, CancellationToken ct = default)
    {
        var upload = await _repository.GetByIdAsync(uploadId, ct)
            ?? throw new UploadNotFoundException(uploadId);

        if (upload.UserId != userId)
            throw new UploadUnauthorizedException();

        return ToSummary(upload);
    }

    public async Task<IEnumerable<VideoUploadSummary>> ListByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var uploads = await _repository.GetByUserIdAsync(userId, ct);
        return uploads.Select(ToSummary);
    }

    // ── mappers ────────────────────────────────────────────────────────────

    private static UploadVideoResponse ToResponse(VideoUpload u) => new(
        u.Id, u.OriginalFileName, u.StoredFileName,
        u.FileSizeBytes, u.Status, u.StoragePath, u.CreatedAt);

    private static VideoUploadSummary ToSummary(VideoUpload u) => new(
        u.Id, u.OriginalFileName, u.FileSizeBytes,
        u.Status, u.ErrorMessage, u.CreatedAt);
}
