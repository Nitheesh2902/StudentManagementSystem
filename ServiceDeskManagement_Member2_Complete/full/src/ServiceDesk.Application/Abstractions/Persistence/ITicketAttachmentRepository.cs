using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Abstractions.Persistence;

public interface ITicketAttachmentRepository
{
    /// <summary>Active attachments of a ticket, read-only.</summary>
    Task<IReadOnlyList<TicketAttachment>> GetByTicketAsync(int ticketId, CancellationToken ct = default);

    /// <summary>Tracked active attachment that belongs to the given ticket.</summary>
    Task<TicketAttachment?> GetByIdAsync(int ticketId, int attachmentId, CancellationToken ct = default);

    Task<int> CountActiveAsync(int ticketId, CancellationToken ct = default);
}
