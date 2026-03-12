using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UploadService.Application.Interfaces;

namespace UploadService.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public string Host     { get; init; } = "localhost";
    public int    Port     { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string Exchange { get; init; } = "video.uploaded";
    public string Queue    { get; init; } = "video.processing";
}

public sealed class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel      _channel;
    private readonly RabbitMqSettings _settings;

    public RabbitMqPublisher(IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port     = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        _connection = factory.CreateConnection();
        _channel    = _connection.CreateModel();

        _channel.ExchangeDeclare(_settings.Exchange, ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(_settings.Queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_settings.Queue, _settings.Exchange, routingKey: string.Empty);
    }

    public Task PublishVideoUploadedAsync(Guid uploadId, Guid userId, string storagePath, CancellationToken ct = default)
    {
        var message = JsonSerializer.Serialize(new
        {
            UploadId    = uploadId,
            UserId      = userId,
            StoragePath = storagePath,
            OccurredAt  = DateTime.UtcNow
        });

        var body  = Encoding.UTF8.GetBytes(message);
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;

        _channel.BasicPublish(
            exchange:   _settings.Exchange,
            routingKey: string.Empty,
            basicProperties: props,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
