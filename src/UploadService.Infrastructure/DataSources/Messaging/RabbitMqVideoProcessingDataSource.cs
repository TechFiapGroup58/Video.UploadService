using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UploadService.Core.Gateways;

namespace UploadService.Infrastructure.DataSources.Messaging;

public sealed class RabbitMqVideoProcessingDataSource(
    IConnection connection,
    IOptions<RabbitMqSettings> options) : IVideoProcessingGateway, IDisposable
{
    private readonly RabbitMqSettings _settings = options.Value;
    private readonly IModel _channel = connection.CreateModel();

    public Task PublishVideoUploadedAsync(
        Guid uploadId,
        Guid userId,
        string storagePath,
        string originalFileName,
        CancellationToken ct = default)
    {
        _channel.ExchangeDeclare(
            exchange: _settings.VideoUploadedExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            queue: _settings.VideoUploadedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: _settings.VideoUploadedQueue,
            exchange: _settings.VideoUploadedExchange,
            routingKey: _settings.VideoUploadedRoutingKey);

        var message = new VideoUploadedMessage(
            uploadId, userId, storagePath, originalFileName, DateTime.UtcNow);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var props = _channel.CreateBasicProperties();
        props.Persistent  = true;
        props.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: _settings.VideoUploadedExchange,
            routingKey: _settings.VideoUploadedRoutingKey,
            basicProperties: props,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose() => _channel.Dispose();
}
