namespace UploadService.Core.Gateways;

public interface IVideoProcessingGateway
{
    /// <summary>Publica mensagem na fila para o ProcessorService processar o vídeo.</summary>
    Task PublishVideoUploadedAsync(
        Guid uploadId,
        Guid userId,
        string storagePath,
        string originalFileName,
        CancellationToken ct = default);
}
