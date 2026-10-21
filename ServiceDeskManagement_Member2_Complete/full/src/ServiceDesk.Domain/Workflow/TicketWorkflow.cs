using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Workflow;

/// <summary>The single source of truth for which status can follow which.</summary>
public static class TicketWorkflow
{
    private static readonly IReadOnlyDictionary<TicketStatus, TicketStatus[]> Map =
        new Dictionary<TicketStatus, TicketStatus[]>
        {
            [TicketStatus.New] = new[] { TicketStatus.Open, TicketStatus.Cancelled },
            [TicketStatus.Open] = new[] { TicketStatus.Assigned, TicketStatus.InProgress, TicketStatus.Cancelled },
            [TicketStatus.Assigned] = new[] { TicketStatus.InProgress },
            [TicketStatus.InProgress] = new[] { TicketStatus.WaitingForUser, TicketStatus.Resolved },
            [TicketStatus.WaitingForUser] = new[] { TicketStatus.InProgress },
            [TicketStatus.Resolved] = new[] { TicketStatus.Closed, TicketStatus.Reopened },
            [TicketStatus.Reopened] = new[] { TicketStatus.InProgress },
            [TicketStatus.Closed] = Array.Empty<TicketStatus>(),
            [TicketStatus.Cancelled] = Array.Empty<TicketStatus>()
        };

    public static IReadOnlyList<TicketStatus> NextFrom(TicketStatus status) => Map[status];

    public static bool IsAllowed(TicketStatus from, TicketStatus to) => Map[from].Contains(to);
}
