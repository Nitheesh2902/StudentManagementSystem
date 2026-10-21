using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Abstractions.Persistence;

public interface IQueueRepository
{
    /// <summary>Tracked entity including ALL its members (active and inactive), for updates.</summary>
    Task<TicketQueue?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Read-only list; each queue carries its active members.</summary>
    Task<IReadOnlyList<TicketQueue>> GetAllAsync(bool includeInactive, CancellationToken ct = default);

    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
    Task<bool> IsActiveMemberAsync(int queueId, int employeeId, CancellationToken ct = default);

    /// <summary>Distinct active agent ids of one queue, or of every queue when queueId is null.</summary>
    Task<IReadOnlyList<int>> GetActiveMemberIdsAsync(int? queueId, CancellationToken ct = default);

    /// <summary>True while the queue still holds tickets that are not Resolved / Closed / Cancelled.</summary>
    Task<bool> HasActiveTicketsAsync(int queueId, CancellationToken ct = default);

    Task AddAsync(TicketQueue queue, CancellationToken ct = default);
}
