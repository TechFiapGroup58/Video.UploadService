using FluentAssertions;
using UploadService.Core.Entities;
using Xunit;

namespace UploadService.UnitTests.Core.Entities;

public sealed class VideoUploadEntityTests
{
    [Fact]
    public void Create_ValidData_SetsDefaultPendingStatus()
    {
        var upload = VideoUpload.Create(Guid.NewGuid(), "video.mp4", "video/mp4", 1024);

        upload.Id.Should().NotBeEmpty();
        upload.Status.Should().Be(VideoUploadStatus.Pending);
        upload.StoragePath.Should().BeEmpty();
        upload.ErrorMessage.Should().BeNull();
        upload.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Create_GeneratesUniqueFileName()
    {
        var a = VideoUpload.Create(Guid.NewGuid(), "video.mp4", "video/mp4", 1024);
        var b = VideoUpload.Create(Guid.NewGuid(), "video.mp4", "video/mp4", 1024);
        a.FileName.Should().NotBe(b.FileName);
    }

    [Fact]
    public void Create_PreservesExtension()
    {
        var upload = VideoUpload.Create(Guid.NewGuid(), "meu video.mkv", "video/x-matroska", 1024);
        upload.FileName.Should().EndWith(".mkv");
    }

    [Fact]
    public void SetStoragePath_ChangesStatusToUploaded()
    {
        var upload = VideoUpload.Create(Guid.NewGuid(), "v.mp4", "video/mp4", 512);
        upload.SetStoragePath("videos/v.mp4");

        upload.Status.Should().Be(VideoUploadStatus.Uploaded);
        upload.StoragePath.Should().Be("videos/v.mp4");
        upload.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsQueued_ChangesStatusToQueued()
    {
        var upload = VideoUpload.Create(Guid.NewGuid(), "v.mp4", "video/mp4", 512);
        upload.SetStoragePath("videos/v.mp4");
        upload.MarkAsQueued();

        upload.Status.Should().Be(VideoUploadStatus.Queued);
        upload.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsFailed_SetsStatusAndMessage()
    {
        var upload = VideoUpload.Create(Guid.NewGuid(), "v.mp4", "video/mp4", 512);
        upload.MarkAsFailed("MinIO indisponível");

        upload.Status.Should().Be(VideoUploadStatus.Failed);
        upload.ErrorMessage.Should().Be("MinIO indisponível");
        upload.UpdatedAt.Should().NotBeNull();
    }
}
