using FluentAssertions;
using Moq;
using UploadService.Core.Entities;
using UploadService.Core.Exceptions;
using UploadService.Core.Gateways;
using UploadService.Core.UseCases.Videos;
using Xunit;

namespace UploadService.UnitTests.Core.UseCases.Videos;

public sealed class GetVideoUseCaseTests
{
    private readonly Mock<IVideoUploadGateway> _gateway = new();
    private readonly GetVideoUseCase          _sut;

    public GetVideoUseCaseTests() => _sut = new GetVideoUseCase(_gateway.Object);

    private static VideoUpload MakeUpload(Guid userId)
    {
        var u = VideoUpload.Create(userId, "v.mp4", "video/mp4", 512);
        u.SetStoragePath("videos/v.mp4");
        return u;
    }

    [Fact]
    public async Task Execute_OwnerRequest_ReturnsVideoOutput()
    {
        var upload = MakeUpload(Guid.NewGuid());
        _gateway.Setup(g => g.FindByIdAsync(upload.Id, default)).ReturnsAsync(upload);

        var result = await _sut.ExecuteAsync(upload.Id, upload.UserId);

        result.Id.Should().Be(upload.Id);
        result.Status.Should().Be(VideoUploadStatus.Uploaded);
    }

    [Fact]
    public async Task Execute_NotFound_ThrowsVideoUploadNotFoundException()
    {
        var id = Guid.NewGuid();
        _gateway.Setup(g => g.FindByIdAsync(id, default)).ReturnsAsync((VideoUpload?)null);

        var act = () => _sut.ExecuteAsync(id, Guid.NewGuid());

        await act.Should().ThrowAsync<VideoUploadNotFoundException>().WithMessage($"*{id}*");
    }

    [Fact]
    public async Task Execute_DifferentUser_ThrowsUnauthorizedUploadException()
    {
        var upload = MakeUpload(Guid.NewGuid());
        _gateway.Setup(g => g.FindByIdAsync(upload.Id, default)).ReturnsAsync(upload);

        var act = () => _sut.ExecuteAsync(upload.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedUploadException>();
    }
}
