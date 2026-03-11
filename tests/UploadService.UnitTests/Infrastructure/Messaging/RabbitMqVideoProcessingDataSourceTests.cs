using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using UploadService.Infrastructure.DataSources.Messaging;
using Xunit;

namespace UploadService.UnitTests.Infrastructure.Messaging;

public sealed class RabbitMqVideoProcessingDataSourceTests
{
    private readonly Mock<IConnection>  _connection = new();
    private readonly Mock<IModel>       _channel    = new();
    private readonly RabbitMqVideoProcessingDataSource _sut;

    public RabbitMqVideoProcessingDataSourceTests()
    {
        _connection.Setup(c => c.CreateModel()).Returns(_channel.Object);
        _channel.Setup(c => c.CreateBasicProperties()).Returns(new Mock<IBasicProperties>().Object);

        _sut = new RabbitMqVideoProcessingDataSource(
            _connection.Object,
            Options.Create(new RabbitMqSettings
            {
                VideoUploadedExchange   = "video.uploaded",
                VideoUploadedQueue      = "video.processing.queue",
                VideoUploadedRoutingKey = "video.uploaded"
            }));
    }

    [Fact]
    public async Task Publish_DeclaresExchangeQueueAndBind()
    {
        await _sut.PublishVideoUploadedAsync(Guid.NewGuid(), Guid.NewGuid(), "videos/v.mp4", "v.mp4");

        _channel.Verify(c => c.ExchangeDeclare(
            "video.uploaded", ExchangeType.Direct, true, false, null), Times.Once);

        _channel.Verify(c => c.QueueDeclare(
            "video.processing.queue", true, false, false, null), Times.Once);

        _channel.Verify(c => c.QueueBind(
            "video.processing.queue", "video.uploaded", "video.uploaded", null), Times.Once);
    }

    [Fact]
    public async Task Publish_CallsBasicPublishWithPersistentMessage()
    {
        var props = new Mock<IBasicProperties>();
        _channel.Setup(c => c.CreateBasicProperties()).Returns(props.Object);

        await _sut.PublishVideoUploadedAsync(Guid.NewGuid(), Guid.NewGuid(), "videos/v.mp4", "v.mp4");

        _channel.Verify(c => c.BasicPublish(
            "video.uploaded",
            "video.uploaded",
            false,
            props.Object,
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
    }
}
