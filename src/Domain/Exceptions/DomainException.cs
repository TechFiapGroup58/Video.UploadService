namespace UploadService.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public sealed class InvalidVideoException : DomainException
{
    public InvalidVideoException(string reason)
        : base($"Arquivo inválido: {reason}") { }
}

public sealed class UploadNotFoundException : DomainException
{
    public UploadNotFoundException(Guid id)
        : base($"Upload '{id}' não encontrado.") { }
}

public sealed class UploadUnauthorizedException : DomainException
{
    public UploadUnauthorizedException()
        : base("Acesso negado ao recurso.") { }
}
