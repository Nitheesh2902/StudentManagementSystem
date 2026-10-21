using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Entities;

/// <summary>One assignment, reassignment or escalation of a ticket (kept for reporting).</summary>
public class TicketAssignment
{
    private TicketAssignment() { } // EF Core

    public TicketAssignment(
        AssignmentType type, int? fromAgentEmployeeId, int? toAgentEmployeeId,
        int fromQueueId, int toQueueId, string? note, int? assignedByUserId)
    {
        Type = type;
        FromAgentEmployeeId = fromAgentEmployeeId;
        ToAgentEmployeeId = toAgentEmployeeId;
        FromQueueId = fromQueueId;
        ToQueueId = toQueueId;
        Note = note is { Length: > 500 } ? note[..500] : note;
        AssignedByUserId = assignedByUserId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int TicketId { get; private set; }
    public AssignmentType Type { get; private set; }
    public int? FromAgentEmployeeId { get; private set; }
    public int? ToAgentEmployeeId { get; private set; }
    public int FromQueueId { get; private set; }
    public int ToQueueId { get; private set; }
    public string? Note { get; private set; }
    public int? AssignedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
