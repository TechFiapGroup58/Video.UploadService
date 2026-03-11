using UploadService.Core.Exceptions;
using UploadService.Core.Gateways;

namespace UploadService.Infrastructure.Security;

public sealed class VideoValidatorDataSource : IVideoValidatorGateway
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/mpeg", "video/avi", "video/x-msvideo",
        "video/quicktime", "video/x-matroska", "video/webm"
    };

    private const long MaxFileSizeBytes = 2L * 1024 * 1024 * 1024; // 2 GB

    public void Validate(string contentType, long fileSizeBytes)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidVideoException(
                $"tipo '{contentType}' não suportado. Use: {string.Join(", ", AllowedContentTypes)}");

        if (fileSizeBytes <= 0)
            throw new InvalidVideoException("o arquivo está vazio.");

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new InvalidVideoException(
                $"tamanho {fileSizeBytes / (1024 * 1024)} MB excede o limite de 2 GB.");
    }
}
