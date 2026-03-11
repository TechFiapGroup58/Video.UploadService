namespace UploadService.Core.Exceptions;

public sealed class UnauthorizedUploadException : DomainException
{
    public UnauthorizedUploadException()
        : base("Acesso não autorizado ao recurso de upload.") { }
}
