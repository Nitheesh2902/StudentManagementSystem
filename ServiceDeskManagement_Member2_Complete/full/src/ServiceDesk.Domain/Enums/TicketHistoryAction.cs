namespace ServiceDesk.Domain.Enums;

public enum TicketHistoryAction
{
    Created = 1,
    Updated = 2,
    PriorityChanged = 3,
    StatusChanged = 4,
    Assigned = 5,
    Reassigned = 6,
    Escalated = 7,
    CommentAdded = 8,
    AttachmentAdded = 9,
    Reopened = 10,
    AttachmentRemoved = 11
}
