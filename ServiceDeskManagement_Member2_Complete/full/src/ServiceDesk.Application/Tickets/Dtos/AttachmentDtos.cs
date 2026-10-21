namespace ServiceDesk.Application.Tickets.Dtos;

public sealed record AttachmentResponse(
    int Id, int TicketId, string FileName, string ContentType, long SizeBytes,
    int UploadedByEmployeeId, DateTime CreatedAtUtc);

public sealed record AttachmentDownload(Stream Content, string FileName, string ContentType);
