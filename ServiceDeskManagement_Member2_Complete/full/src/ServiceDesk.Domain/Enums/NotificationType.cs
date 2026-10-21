namespace ServiceDesk.Domain.Enums;

public enum NotificationType
{
    TicketCreated = 1,
    TicketAssigned = 2,
    StatusChanged = 3,
    PriorityChanged = 4,
    SlaRisk = 5,
    SlaBreached = 6,
    TicketResolved = 7,
    TicketClosed = 8,
    TicketEscalated = 9,
    CommentAdded = 10
}
