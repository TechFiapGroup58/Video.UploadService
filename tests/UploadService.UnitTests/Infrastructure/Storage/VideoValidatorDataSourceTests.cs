using FluentAssertions;
using UploadService.Core.Exceptions;
using UploadService.Infrastructure.Security;
using Xunit;

namespace UploadService.UnitTests.Infrastructure.Storage;

public sealed class VideoValidatorDataSourceTests
{
    private readonly VideoValidatorDataSource _sut = new();

    [Theory]
    [InlineData("video/mp4")]
    [InlineData("video/mpeg")]
    [InlineData("video/avi")]
    [InlineData("video/quicktime")]
    [InlineData("video/webm")]
    [InlineData("video/x-matroska")]
    public void Validate_AllowedContentTypes_DoesNotThrow(string contentType)
    {
        var act = () => _sut.Validate(contentType, 1024);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_UnsupportedType_ThrowsInvalidVideoException()
    {
        var act = () => _sut.Validate("image/png", 1024);
        act.Should().Throw<InvalidVideoException>().WithMessage("*image/png*");
    }

    [Fact]
    public void Validate_ZeroSize_ThrowsInvalidVideoException()
    {
        var act = () => _sut.Validate("video/mp4", 0);
        act.Should().Throw<InvalidVideoException>().WithMessage("*vazio*");
    }

    [Fact]
    public void Validate_NegativeSize_ThrowsInvalidVideoException()
    {
        var act = () => _sut.Validate("video/mp4", -1);
        act.Should().Throw<InvalidVideoException>();
    }

    [Fact]
    public void Validate_ExceedsMaxSize_ThrowsInvalidVideoException()
    {
        var overLimit = 2L * 1024 * 1024 * 1024 + 1;
        var act = () => _sut.Validate("video/mp4", overLimit);
        act.Should().Throw<InvalidVideoException>().WithMessage("*limite*");
    }

    [Fact]
    public void Validate_ExactlyAtLimit_DoesNotThrow()
    {
        var atLimit = 2L * 1024 * 1024 * 1024;
        var act = () => _sut.Validate("video/mp4", atLimit);
        act.Should().NotThrow();
    }
}
