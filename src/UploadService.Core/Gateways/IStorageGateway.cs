namespace UploadService.Core.Gateways;

public interface IStorageGateway
{
    /// <summary>Salva o stream no storage e retorna o path/key do objeto.</summary>
    Task<string> UploadAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken ct = default);

    /// <summary>Retorna uma URL pré-assinada para download temporário.</summary>
    Task<string> GetPresignedUrlAsync(string storagePath, TimeSpan expiry, CancellationToken ct = default);

    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
