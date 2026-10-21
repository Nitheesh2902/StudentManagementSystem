using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.Entities;

/// <summary>An agent (employee) who works tickets of a queue. Employees are owned by Member 1, so only the id is stored.</summary>
public class TicketQueueMember : BaseEntity
{
    private TicketQueueMember() { } // EF Core

    public TicketQueueMember(int agentEmployeeId)
    {
        if (agentEmployeeId <= 0) throw new DomainException("Agent is required.");
        AgentEmployeeId = agentEmployeeId;
    }

    public int QueueId { get; private set; }
    public int AgentEmployeeId { get; private set; }
}
