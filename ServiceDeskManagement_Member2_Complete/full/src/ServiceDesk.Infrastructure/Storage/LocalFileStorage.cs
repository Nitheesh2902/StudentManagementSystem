using Microsoft.Extensions.Configuration;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Infrastructure.Storage;

/// <summary>Stores attachments on local disk (default: App_Data/attachments next to the app, or Storage:AttachmentsPath).</summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration configuration)
    {
        var configured = configuration["Storage:AttachmentsPath"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "attachments")
            : configured;

        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string storedFileName, Stream content, CancellationToken ct = default)
    {
        var path = PathFor(storedFileName);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken ct = default)
    {
        var path = PathFor(storedFileName);
        if (!File.Exists(path))
            throw new NotFoundException("Attachment file", storedFileName);

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    // GetFileName strips any directory part, so a stored name can never escape the storage folder.
    private string PathFor(string storedFileName) => Path.Combine(_root, Path.GetFileName(storedFileName));
}
