namespace ServiceDesk.Application.Abstractions;

/// <summary>Stores attachment files. The implementation decides where (disk, blob storage...).</summary>
public interface IFileStorage
{
    Task SaveAsync(string storedFileName, Stream content, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct = default);
}
