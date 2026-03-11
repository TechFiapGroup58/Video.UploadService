namespace UploadService.Core.Exceptions;

public sealed class VideoUploadNotFoundException : DomainException
{
    public VideoUploadNotFoundException(Guid id)
        : base($"Upload '{id}' não encontrado.") { }
}
