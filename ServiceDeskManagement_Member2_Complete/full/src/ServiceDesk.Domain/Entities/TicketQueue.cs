using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.Entities;

/// <summary>A team queue that tickets are routed to.</summary>
public class TicketQueue : BaseEntity
{
    /// <summary>Seeded fallback queue; always active.</summary>
    public const int GeneralQueueId = 1;

    private TicketQueue() { } // EF Core

    public TicketQueue(string name, string? description, int? departmentId, int? managerEmployeeId)
    {
        Apply(name, description, departmentId, managerEmployeeId);
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    // Cross-module references (Member 1): ids only, no FK yet.
    public int? DepartmentId { get; private set; }
    public int? ManagerEmployeeId { get; private set; }

    public ICollection<TicketQueueMember> Members { get; private set; } = new List<TicketQueueMember>();

    public IReadOnlyList<int> ActiveMemberIds =>
        Members.Where(m => m.IsActive).Select(m => m.AgentEmployeeId).OrderBy(id => id).ToList();

    public void Update(string name, string? description, int? departmentId, int? managerEmployeeId)
        => Apply(name, description, departmentId, managerEmployeeId);

    public override void Deactivate()
    {
        if (Id == GeneralQueueId)
            throw new DomainException("The general queue cannot be deactivated.");
        base.Deactivate();
    }

    public void AddMember(int agentEmployeeId)
    {
        if (agentEmployeeId <= 0) throw new DomainException("Agent is required.");

        var existing = Members.FirstOrDefault(m => m.AgentEmployeeId == agentEmployeeId);
        if (existing is null)
        {
            Members.Add(new TicketQueueMember(agentEmployeeId));
            return;
        }

        if (existing.IsActive)
            throw new ConflictException($"Employee {agentEmployeeId} is already a member of queue '{Name}'.");

        existing.Activate();
    }

    public void RemoveMember(int agentEmployeeId)
    {
        var member = Members.FirstOrDefault(m => m.AgentEmployeeId == agentEmployeeId && m.IsActive)
                     ?? throw new NotFoundException("Queue member", agentEmployeeId);
        member.Deactivate();
    }

    private void Apply(string name, string? description, int? departmentId, int? managerEmployeeId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Queue name is required.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DepartmentId = departmentId;
        ManagerEmployeeId = managerEmployeeId;
    }
}
