using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.UnitTests;

internal static class Make
{
    public static readonly DateTime Now = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    public static void SetId(BaseEntity entity, int id)
        => typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(entity, id);

    public static TicketQueue Queue(int id = 1, string name = "General Service Desk")
    {
        var queue = new TicketQueue(name, null, null, null);
        SetId(queue, id);
        return queue;
    }

    public static ServiceItem Service(int? defaultQueueId = null)
    {
        var category = new TicketCategory("IT Support", null);
        SetId(category, 1);
        var service = new ServiceItem("VPN access", null, category, TicketPriority.Medium, defaultQueueId);
        SetId(service, 1);
        return service;
    }

    public static Ticket Ticket(int requester = 10, TicketQueue? queue = null, TicketPriority priority = TicketPriority.Medium)
    {
        var ticket = new Ticket(
            "TKT-2026-000001", "Cannot connect", "VPN fails on login", requester,
            Service(), queue ?? Queue(), priority, createdByUserId: 1);
        SetId(ticket, 1);
        return ticket;
    }

    /// <summary>New -> Open -> Assigned -> InProgress, worked by agent 5.</summary>
    public static Ticket InProgressTicket(int requester = 10)
    {
        var ticket = Ticket(requester);
        ticket.AssignTo(5, null, 5, Now);
        ticket.ChangeStatus(TicketStatus.InProgress, null, Now, 5);
        return ticket;
    }

    public static Ticket ResolvedTicket(DateTime resolvedAtUtc, int requester = 10)
    {
        var ticket = InProgressTicket(requester);
        ticket.ChangeStatus(TicketStatus.Resolved, "Reset the VPN profile", resolvedAtUtc, 5);
        return ticket;
    }
}

internal sealed class FakeUser : ICurrentUserService
{
    public int? UserId { get; set; } = 1;
    public int? EmployeeId { get; set; } = 10;
    public string? Role { get; set; } = "Employee";
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
}

internal sealed class FakePolicy : ITicketPolicy
{
    public int ReopenWindowDays { get; set; } = 7;
}

internal sealed class FakeTicketRepository : ITicketRepository
{
    private readonly Ticket? _ticket;
    public FakeTicketRepository(Ticket? ticket) => _ticket = ticket;

    public Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_ticket is not null && _ticket.Id == id ? _ticket : null);

    public Task<(IReadOnlyList<Ticket> Items, int TotalCount)> GetPagedAsync(
        TicketFilter filter, int page, int pageSize, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<TicketHistory>> GetHistoryAsync(int ticketId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<AgentWorkloadRow>> GetAgentWorkloadAsync(int? queueId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task AddAsync(Ticket ticket, CancellationToken ct = default)
        => throw new NotImplementedException();
}

internal sealed class FakeQueueRepository : IQueueRepository
{
    private readonly HashSet<(int QueueId, int EmployeeId)> _members;

    public FakeQueueRepository(params (int QueueId, int EmployeeId)[] members)
        => _members = new HashSet<(int, int)>(members);

    public Task<bool> IsActiveMemberAsync(int queueId, int employeeId, CancellationToken ct = default)
        => Task.FromResult(_members.Contains((queueId, employeeId)));

    public Task<TicketQueue?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<TicketQueue>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<int>> GetActiveMemberIdsAsync(int? queueId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<bool> HasActiveTicketsAsync(int queueId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task AddAsync(TicketQueue queue, CancellationToken ct = default)
        => throw new NotImplementedException();
}
