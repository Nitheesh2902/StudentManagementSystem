using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.Entities;

public class TicketAttachment : BaseEntity
{
    private TicketAttachment() { } // EF Core

    public TicketAttachment(
        string fileName, string storedFileName, string contentType, long sizeBytes, int uploadedByEmployeeId)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new DomainException("File name is required.");
        if (string.IsNullOrWhiteSpace(storedFileName)) throw new DomainException("Stored file name is required.");
        if (sizeBytes <= 0) throw new DomainException("The file is empty.");

        FileName = fileName;
        StoredFileName = storedFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedByEmployeeId = uploadedByEmployeeId;
    }

    public int TicketId { get; private set; }

    /// <summary>The (sanitised) name the user uploaded.</summary>
    public string FileName { get; private set; } = default!;

    /// <summary>The generated name on disk. Never exposed through the API.</summary>
    public string StoredFileName { get; private set; } = default!;

    public string ContentType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public int UploadedByEmployeeId { get; private set; }
}
