using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Abstractions.Persistence;

public sealed record TicketFilter(
    string? Search,
    TicketStatus? Status,
    TicketPriority? Priority,
    int? CategoryId,
    int? ServiceItemId,
    int? QueueId,
    int? AssignedAgentEmployeeId,
    bool UnassignedOnly,
    int? RequesterEmployeeId,
    int? VisibleToAgentEmployeeId,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToExclusiveUtc);

public sealed record AgentWorkloadRow(
    int? AgentEmployeeId, int Assigned, int InProgress, int WaitingForUser, int Reopened, int HighPriority);

public interface ITicketRepository
{
    /// <summary>Tracked entity including Category, ServiceItem and Queue, for updates.</summary>
    Task<Ticket?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(IReadOnlyList<Ticket> Items, int TotalCount)> GetPagedAsync(
        TicketFilter filter, int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<TicketHistory>> GetHistoryAsync(int ticketId, CancellationToken ct = default);

    /// <summary>Active (Assigned / InProgress / WaitingForUser / Reopened) tickets grouped per assigned agent.</summary>
    Task<IReadOnlyList<AgentWorkloadRow>> GetAgentWorkloadAsync(int? queueId, CancellationToken ct = default);

    Task AddAsync(Ticket ticket, CancellationToken ct = default);
}
