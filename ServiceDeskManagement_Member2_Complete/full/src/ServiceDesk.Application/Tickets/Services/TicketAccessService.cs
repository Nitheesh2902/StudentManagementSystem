using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Application.Common;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Tickets.Services;

public class TicketAccessService : ITicketAccessService
{
    private readonly ITicketRepository _tickets;
    private readonly IQueueRepository _queues;
    private readonly ICurrentUserService _user;

    public TicketAccessService(ITicketRepository tickets, IQueueRepository queues, ICurrentUserService user)
    {
        _tickets = tickets;
        _queues = queues;
        _user = user;
    }

    public async Task<Ticket> GetAccessibleAsync(int id, CancellationToken ct = default)
    {
        var employeeId = _user.RequireEmployeeId();

        var ticket = await _tickets.GetByIdAsync(id, ct)
                     ?? throw new NotFoundException(nameof(Ticket), id);

        if (!await CanViewCoreAsync(ticket, employeeId, ct))
            throw new NotFoundException(nameof(Ticket), id);

        return ticket;
    }

    public Task<bool> CanViewAsync(Ticket ticket, CancellationToken ct = default)
        => CanViewCoreAsync(ticket, _user.RequireEmployeeId(), ct);

    private async Task<bool> CanViewCoreAsync(Ticket ticket, int employeeId, CancellationToken ct)
    {
        if (ticket.RequesterEmployeeId == employeeId) return true;
        if (_user.IsManagerOrAdmin()) return true;

        if (_user.IsInRole(Roles.Agent))
        {
            return ticket.AssignedAgentEmployeeId == employeeId
                   || await _queues.IsActiveMemberAsync(ticket.QueueId, employeeId, ct);
        }

        return false;
    }
}
