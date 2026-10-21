using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Common;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Tickets.Services;

public class TicketResponseBuilder : ITicketResponseBuilder
{
    private readonly ICurrentUserService _user;
    private readonly ITicketPolicy _policy;

    public TicketResponseBuilder(ICurrentUserService user, ITicketPolicy policy)
    {
        _user = user;
        _policy = policy;
    }

    public TicketResponse Build(Ticket t)
    {
        var window = TimeSpan.FromDays(_policy.ReopenWindowDays);

        return new TicketResponse(
            t.Id, t.TicketNumber, t.Title, t.Description, t.RequesterEmployeeId,
            t.CategoryId, t.Category.Name, t.ServiceItemId, t.ServiceItem.Name,
            t.QueueId, t.Queue.Name, t.AssignedAgentEmployeeId, t.IsEscalated,
            t.Priority, t.Status,
            t.DueResponseAtUtc, t.DueResolutionAtUtc, t.ResolvedAtUtc, t.ClosedAtUtc,
            t.ResolutionNote, t.ReopenCount,
            t.AssetId, t.CreatedAtUtc, t.UpdatedAtUtc,
            TicketStatusPermissions.AllowedFor(t, _user.IsStaff()),
            t.CanReopen(DateTime.UtcNow, window));
    }
}
