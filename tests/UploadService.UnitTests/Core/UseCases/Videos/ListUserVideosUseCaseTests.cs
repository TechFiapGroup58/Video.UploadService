using FluentAssertions;
using Moq;
using UploadService.Core.Entities;
using UploadService.Core.Gateways;
using UploadService.Core.UseCases.Videos;
using Xunit;

namespace UploadService.UnitTests.Core.UseCases.Videos;

public sealed class ListUserVideosUseCaseTests
{
    private readonly Mock<IVideoUploadGateway> _gateway = new();
    private readonly ListUserVideosUseCase     _sut;

    public ListUserVideosUseCaseTests() => _sut = new ListUserVideosUseCase(_gateway.Object);

    [Fact]
    public async Task Execute_ReturnsAllUserVideos()
    {
        var userId = Guid.NewGuid();
        var uploads = new List<VideoUpload>
        {
            VideoUpload.Create(userId, "a.mp4", "video/mp4", 100),
            VideoUpload.Create(userId, "b.mp4", "video/mp4", 200),
        };
        _gateway.Setup(g => g.FindByUserIdAsync(userId, default)).ReturnsAsync(uploads);

        var result = await _sut.ExecuteAsync(userId);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(v => v.UserId == userId);
    }

    [Fact]
    public async Task Execute_NoVideos_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        _gateway.Setup(g => g.FindByUserIdAsync(userId, default))
                .ReturnsAsync(new List<VideoUpload>());

        var result = await _sut.ExecuteAsync(userId);

        result.Should().BeEmpty();
    }
}
