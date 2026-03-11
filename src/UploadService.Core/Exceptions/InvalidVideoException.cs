namespace UploadService.Core.Exceptions;

public sealed class InvalidVideoException : DomainException
{
    public InvalidVideoException(string reason)
        : base($"Arquivo de vídeo inválido: {reason}") { }
}
