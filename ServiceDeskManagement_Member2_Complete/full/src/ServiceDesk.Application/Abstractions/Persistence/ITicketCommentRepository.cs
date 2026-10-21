using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Abstractions.Persistence;

public interface ITicketCommentRepository
{
    Task<IReadOnlyList<TicketComment>> GetByTicketAsync(int ticketId, bool includeInternal, CancellationToken ct = default);
}
