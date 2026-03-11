namespace UploadService.Core.Exceptions;

public sealed class StorageException : DomainException
{
    public StorageException(string message) : base(message) { }
}
