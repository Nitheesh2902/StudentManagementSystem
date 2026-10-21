using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Workflow;

namespace ServiceDesk.Application.Tickets;

/// <summary>Who may request which status change (on top of what the workflow allows).</summary>
public static class TicketStatusPermissions
{
    public static IReadOnlyList<TicketStatus> AllowedFor(Ticket ticket, bool isStaff)
    {
        // Reopened has its own endpoint; Assigned is set by the assignment action.
        var next = TicketWorkflow.NextFrom(ticket.Status)
            .Where(s => s is not (TicketStatus.Reopened or TicketStatus.Assigned));

        if (isStaff)
            return next.ToList();

        // The requester may cancel (New/Open), accept a resolution (close), or reply to a request for information.
        return next.Where(s => s switch
        {
            TicketStatus.Cancelled => true,
            TicketStatus.Closed => true,
            TicketStatus.InProgress => ticket.Status == TicketStatus.WaitingForUser,
            _ => false
        }).ToList();
    }
}
