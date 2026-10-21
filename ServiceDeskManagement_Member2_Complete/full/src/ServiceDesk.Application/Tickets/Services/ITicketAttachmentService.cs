using ServiceDesk.Application.Tickets.Dtos;

namespace ServiceDesk.Application.Tickets.Services;

public interface ITicketAttachmentService
{
    Task<IReadOnlyList<AttachmentResponse>> ListAsync(int ticketId, CancellationToken ct = default);
    Task<AttachmentResponse> UploadAsync(int ticketId, string fileName, Stream content, CancellationToken ct = default);
    Task<AttachmentDownload> DownloadAsync(int ticketId, int attachmentId, CancellationToken ct = default);
    Task DeleteAsync(int ticketId, int attachmentId, CancellationToken ct = default);
}
