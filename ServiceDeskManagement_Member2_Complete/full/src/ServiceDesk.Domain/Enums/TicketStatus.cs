namespace ServiceDesk.Domain.Enums;

public enum TicketStatus
{
    New = 1,
    Open = 2,
    Assigned = 3,
    InProgress = 4,
    WaitingForUser = 5,
    Resolved = 6,
    Closed = 7,
    Reopened = 8,
    Cancelled = 9
}
