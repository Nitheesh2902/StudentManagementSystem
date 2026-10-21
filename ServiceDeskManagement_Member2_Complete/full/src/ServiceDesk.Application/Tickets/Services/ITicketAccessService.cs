using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Tickets.Services;

/// <summary>
/// One place that decides who may see a ticket:
/// employees their own, agents their queues' tickets (or ones assigned to them), managers/admins everything.
/// </summary>
public interface ITicketAccessService
{
    /// <summary>Loads the ticket or throws NotFound (also when the user may not see it, so existence is not leaked).</summary>
    Task<Ticket> GetAccessibleAsync(int id, CancellationToken ct = default);

    Task<bool> CanViewAsync(Ticket ticket, CancellationToken ct = default);
}
