using UploadService.Application.Interfaces;
using UploadService.Domain.Exceptions;

namespace UploadService.Infrastructure.Storage;

public sealed class VideoValidator : IVideoValidator
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/mpeg", "video/avi",
        "video/quicktime", "video/x-matroska", "video/webm"
    };

    private const long MaxBytes = 2L * 1024 * 1024 * 1024; // 2 GB

    public void Validate(string contentType, long fileSizeBytes)
    {
        if (!AllowedTypes.Contains(contentType))
            throw new InvalidVideoException($"tipo '{contentType}' não permitido.");

        if (fileSizeBytes <= 0)
            throw new InvalidVideoException("arquivo vazio.");

        if (fileSizeBytes > MaxBytes)
            throw new InvalidVideoException("tamanho excede o limite de 2 GB.");
    }
}
