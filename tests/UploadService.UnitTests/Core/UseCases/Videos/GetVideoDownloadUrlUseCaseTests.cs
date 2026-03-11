using FluentAssertions;
using Moq;
using UploadService.Core.Entities;
using UploadService.Core.Exceptions;
using UploadService.Core.Gateways;
using UploadService.Core.UseCases.Videos;
using Xunit;

namespace UploadService.UnitTests.Core.UseCases.Videos;

public sealed class GetVideoDownloadUrlUseCaseTests
{
    private readonly Mock<IVideoUploadGateway> _uploadGw  = new();
    private readonly Mock<IStorageGateway>     _storageGw = new();
    private readonly GetVideoDownloadUrlUseCase _sut;

    public GetVideoDownloadUrlUseCaseTests() =>
        _sut = new GetVideoDownloadUrlUseCase(_uploadGw.Object, _storageGw.Object);

    private static VideoUpload MakeUploadedVideo(Guid userId)
    {
        var u = VideoUpload.Create(userId, "v.mp4", "video/mp4", 512);
        u.SetStoragePath("videos/v.mp4");
        return u;
    }

    [Fact]
    public async Task Execute_UploadedVideo_ReturnsPresignedUrl()
    {
        var upload = MakeUploadedVideo(Guid.NewGuid());
        _uploadGw.Setup(g => g.FindByIdAsync(upload.Id, default)).ReturnsAsync(upload);
        _storageGw.Setup(s => s.GetPresignedUrlAsync("videos/v.mp4", It.IsAny<TimeSpan>(), default))
                  .ReturnsAsync("https://minio/presigned");

        var url = await _sut.ExecuteAsync(upload.Id, upload.UserId);

        url.Should().Be("https://minio/presigned");
    }

    [Fact]
    public async Task Execute_PendingVideo_ThrowsInvalidVideoException()
    {
        var userId = Guid.NewGuid();
        var upload = VideoUpload.Create(userId, "v.mp4", "video/mp4", 512); // status = Pending
        _uploadGw.Setup(g => g.FindByIdAsync(upload.Id, default)).ReturnsAsync(upload);

        var act = () => _sut.ExecuteAsync(upload.Id, userId);

        await act.Should().ThrowAsync<InvalidVideoException>();
    }

    [Fact]
    public async Task Execute_NotFound_ThrowsVideoUploadNotFoundException()
    {
        var id = Guid.NewGuid();
        _uploadGw.Setup(g => g.FindByIdAsync(id, default)).ReturnsAsync((VideoUpload?)null);

        var act = () => _sut.ExecuteAsync(id, Guid.NewGuid());

        await act.Should().ThrowAsync<VideoUploadNotFoundException>();
    }

    [Fact]
    public async Task Execute_DifferentUser_ThrowsUnauthorizedUploadException()
    {
        var upload = MakeUploadedVideo(Guid.NewGuid());
        _uploadGw.Setup(g => g.FindByIdAsync(upload.Id, default)).ReturnsAsync(upload);

        var act = () => _sut.ExecuteAsync(upload.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedUploadException>();
    }

    [Fact]
    public async Task Execute_QueuedVideo_ReturnsUrl()
    {
        var upload = MakeUploadedVideo(Guid.NewGuid());
        upload.MarkAsQueued();
        _uploadGw.Setup(g => g.FindByIdAsync(upload.Id, default)).ReturnsAsync(upload);
        _storageGw.Setup(s => s.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), default))
                  .ReturnsAsync("https://minio/queued");

        var url = await _sut.ExecuteAsync(upload.Id, upload.UserId);

        url.Should().Be("https://minio/queued");
    }
}
