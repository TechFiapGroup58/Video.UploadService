using FluentAssertions;
using Moq;
using UploadService.Core.Entities;
using UploadService.Core.Exceptions;
using UploadService.Core.Gateways;
using UploadService.Core.UseCases.Uploads;
using Xunit;

namespace UploadService.UnitTests.Core.UseCases.Uploads;

public sealed class UploadVideoUseCaseTests
{
    private readonly Mock<IVideoUploadGateway>      _uploadGw    = new();
    private readonly Mock<IStorageGateway>          _storageGw   = new();
    private readonly Mock<IVideoProcessingGateway>  _processingGw = new();
    private readonly Mock<IVideoValidatorGateway>   _validator   = new();
    private readonly UploadVideoUseCase             _sut;

    public UploadVideoUseCaseTests()
    {
        _storageGw.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), default))
                  .ReturnsAsync("videos/file.mp4");

        _sut = new UploadVideoUseCase(_uploadGw.Object, _storageGw.Object, _processingGw.Object, _validator.Object);
    }

    private static UploadVideoInput MakeInput(string contentType = "video/mp4", long size = 1024) =>
        new(Guid.NewGuid(), "video.mp4", contentType, size, new MemoryStream(new byte[size < 100 ? (int)size : 100]));

    [Fact]
    public async Task Execute_ValidInput_ReturnsQueuedOutput()
    {
        var input  = MakeInput();
        var output = await _sut.ExecuteAsync(input);

        output.Status.Should().Be(VideoUploadStatus.Queued);
        output.UserId.Should().Be(input.UserId);
        output.OriginalFileName.Should().Be("video.mp4");
    }

    [Fact]
    public async Task Execute_ValidInput_SavesInitialAndUpdatesStatus()
    {
        await _sut.ExecuteAsync(MakeInput());

        // Save inicial (Pending) + 2 Updates (Uploaded, Queued)
        _uploadGw.Verify(u => u.SaveAsync(It.IsAny<VideoUpload>(), default), Times.Once);
        _uploadGw.Verify(u => u.UpdateAsync(It.IsAny<VideoUpload>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task Execute_ValidInput_PublishesMessage()
    {
        await _sut.ExecuteAsync(MakeInput());

        _processingGw.Verify(p => p.PublishVideoUploadedAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(),
            "videos/file.mp4", "video.mp4", default), Times.Once);
    }

    [Fact]
    public async Task Execute_InvalidContentType_ThrowsInvalidVideoException()
    {
        _validator.Setup(v => v.Validate("text/plain", It.IsAny<long>()))
                  .Throws(new InvalidVideoException("tipo não suportado"));

        var act = () => _sut.ExecuteAsync(MakeInput("text/plain"));

        await act.Should().ThrowAsync<InvalidVideoException>();
        _uploadGw.Verify(u => u.SaveAsync(It.IsAny<VideoUpload>(), default), Times.Never);
    }

    [Fact]
    public async Task Execute_StorageFails_MarksUploadAsFailed()
    {
        _storageGw.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), default))
                  .ThrowsAsync(new StorageException("MinIO indisponível"));

        var act = () => _sut.ExecuteAsync(MakeInput());

        await act.Should().ThrowAsync<StorageException>();

        _uploadGw.Verify(u => u.UpdateAsync(
            It.Is<VideoUpload>(v => v.Status == VideoUploadStatus.Failed), default), Times.Once);
    }

    [Fact]
    public async Task Execute_StorageFails_NeverPublishesMessage()
    {
        _storageGw.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), default))
                  .ThrowsAsync(new StorageException("erro"));

        await Assert.ThrowsAsync<StorageException>(() => _sut.ExecuteAsync(MakeInput()));

        _processingGw.Verify(p => p.PublishVideoUploadedAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }
}
