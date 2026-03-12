using FluentAssertions;
using Moq;
using UploadService.Application.Interfaces;
using UploadService.Application.Services;
using UploadService.Domain.Entities;
using UploadService.Domain.Exceptions;
using Xunit;

namespace UploadService.Tests.Unit.Application;

public sealed class UploadServiceTests
{
    private readonly Mock<IVideoUploadRepository> _repoMock      = new();
    private readonly Mock<IStorageService>        _storageMock   = new();
    private readonly Mock<IMessagePublisher>      _publisherMock = new();
    private readonly Mock<IVideoValidator>        _validatorMock = new();
    private readonly UploadService.Application.Services.UploadService _sut;

    private static readonly Guid   UserId   = Guid.NewGuid();
    private static readonly string FileName = "video.mp4";
    private static readonly string MimeType = "video/mp4";
    private static readonly long   FileSize = 1024 * 1024;

    public UploadServiceTests()
    {
        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), default))
            .ReturnsAsync("videos/stored-file.mp4");

        _sut = new UploadService.Application.Services.UploadService(
            _repoMock.Object,
            _storageMock.Object,
            _publisherMock.Object,
            _validatorMock.Object);
    }

    // ── Upload ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_ValidFile_ReturnsResponse()
    {
        var stream = new MemoryStream(new byte[100]);

        var result = await _sut.UploadAsync(UserId, FileName, MimeType, FileSize, stream);

        result.Should().NotBeNull();
        result.OriginalFileName.Should().Be(FileName);
        result.Status.Should().Be(UploadStatus.Uploaded);
        result.StoragePath.Should().Be("videos/stored-file.mp4");
    }

    [Fact]
    public async Task Upload_ValidFile_CallsValidatorOnce()
    {
        await _sut.UploadAsync(UserId, FileName, MimeType, FileSize, new MemoryStream());

        _validatorMock.Verify(v => v.Validate(MimeType, FileSize), Times.Once);
    }

    [Fact]
    public async Task Upload_ValidFile_SavesAndUpdatesRepository()
    {
        await _sut.UploadAsync(UserId, FileName, MimeType, FileSize, new MemoryStream());

        _repoMock.Verify(r => r.AddAsync(It.IsAny<VideoUpload>(), default),    Times.Once);
        // 1 Update: após storage (Uploaded)
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<VideoUpload>(), default), Times.Once);
    }

    [Fact]
    public async Task Upload_ValidFile_PublishesMessage()
    {
        await _sut.UploadAsync(UserId, FileName, MimeType, FileSize, new MemoryStream());

        _publisherMock.Verify(p => p.PublishVideoUploadedAsync(
            It.IsAny<Guid>(), UserId, "videos/stored-file.mp4", default), Times.Once);
    }

    [Fact]
    public async Task Upload_ValidatorThrows_DoesNotSaveToStorage()
    {
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>()))
            .Throws(new InvalidVideoException("tipo inválido"));

        var act = () => _sut.UploadAsync(UserId, FileName, "image/png", FileSize, new MemoryStream());

        await act.Should().ThrowAsync<InvalidVideoException>();
        _storageMock.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), default), Times.Never);
    }

    [Fact]
    public async Task Upload_StorageThrows_MarksUploadAsFailed()
    {
        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), default))
            .ThrowsAsync(new Exception("MinIO indisponível"));

        var act = () => _sut.UploadAsync(UserId, FileName, MimeType, FileSize, new MemoryStream());

        await act.Should().ThrowAsync<Exception>();

        _repoMock.Verify(r => r.UpdateAsync(
            It.Is<VideoUpload>(v => v.Status == UploadStatus.Failed), default), Times.Once);
    }

    [Fact]
    public async Task Upload_StorageThrows_NeverPublishesMessage()
    {
        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), default))
            .ThrowsAsync(new Exception("erro"));

        await Assert.ThrowsAsync<Exception>(() =>
            _sut.UploadAsync(UserId, FileName, MimeType, FileSize, new MemoryStream()));

        _publisherMock.Verify(p => p.PublishVideoUploadedAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_OwnUpload_ReturnsSummary()
    {
        var upload = VideoUpload.Create(UserId, FileName, MimeType, FileSize);
        _repoMock.Setup(r => r.GetByIdAsync(upload.Id, default)).ReturnsAsync(upload);

        var result = await _sut.GetByIdAsync(upload.Id, UserId);

        result.Id.Should().Be(upload.Id);
        result.OriginalFileName.Should().Be(FileName);
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsUploadNotFoundException()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((VideoUpload?)null);

        var act = () => _sut.GetByIdAsync(id, UserId);

        await act.Should().ThrowAsync<UploadNotFoundException>().WithMessage($"*{id}*");
    }

    [Fact]
    public async Task GetById_AnotherUsersUpload_ThrowsUnauthorizedAccessException()
    {
        var upload = VideoUpload.Create(Guid.NewGuid(), FileName, MimeType, FileSize);
        _repoMock.Setup(r => r.GetByIdAsync(upload.Id, default)).ReturnsAsync(upload);

        var act = () => _sut.GetByIdAsync(upload.Id, UserId);

        await act.Should().ThrowAsync<UploadUnauthorizedException>();
    }

    // ── ListByUser ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ListByUser_ReturnsAllUserUploads()
    {
        var uploads = new List<VideoUpload>
        {
            VideoUpload.Create(UserId, "a.mp4", MimeType, 100),
            VideoUpload.Create(UserId, "b.mp4", MimeType, 200),
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(UserId, default)).ReturnsAsync(uploads);

        var result = await _sut.ListByUserAsync(UserId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListByUser_NoUploads_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetByUserIdAsync(UserId, default))
                 .ReturnsAsync(new List<VideoUpload>());

        var result = await _sut.ListByUserAsync(UserId);

        result.Should().BeEmpty();
    }
}
