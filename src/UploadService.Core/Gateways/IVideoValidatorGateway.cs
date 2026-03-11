namespace UploadService.Core.Gateways;

public interface IVideoValidatorGateway
{
    /// <summary>Verifica content-type e tamanho máximo permitido.</summary>
    void Validate(string contentType, long fileSizeBytes);
}
