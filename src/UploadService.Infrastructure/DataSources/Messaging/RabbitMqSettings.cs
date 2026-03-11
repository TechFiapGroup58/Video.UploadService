namespace UploadService.Infrastructure.DataSources.Messaging;

public sealed class RabbitMqSettings
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public string VideoUploadedExchange { get; init; } = "video.uploaded";
    public string VideoUploadedQueue { get; init; } = "video.processing.queue";
    public string VideoUploadedRoutingKey { get; init; } = "video.uploaded";
}
