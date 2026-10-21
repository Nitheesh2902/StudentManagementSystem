
using EmployeeServiceDesk.Application.Abstractions.Persistence;

using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace EmployeeServiceDesk.Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;

    public TicketRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Ticket?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return _db.Tickets
            .Include(t => t.Category)
            .Include(t => t.ServiceItem)
            .Include(t => t.Queue)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<(IReadOnlyList<Ticket> Items, int TotalCount)> GetPagedAsync(
        TicketFilter filter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Tickets
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.ServiceItem)
            .Include(t => t.Queue)
            .AsQueryable();

        if (filter.RequesterEmployeeId.HasValue)
        {
            query = query.Where(t =>
                t.RequesterEmployeeId == filter.RequesterEmployeeId.Value);
        }

        if (filter.VisibleToAgentEmployeeId.HasValue)
        {
            var agentId = filter.VisibleToAgentEmployeeId.Value;

            query = query.Where(t =>
                t.RequesterEmployeeId == agentId
                || t.AssignedAgentEmployeeId == agentId
                || _db.TicketQueueMembers.Any(m =>
                    m.QueueId == t.QueueId
                    && m.AgentEmployeeId == agentId
                    && m.IsActive));
        }

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(t => t.Priority == filter.Priority.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);

        if (filter.ServiceItemId.HasValue)
            query = query.Where(t => t.ServiceItemId == filter.ServiceItemId.Value);

        if (filter.QueueId.HasValue)
            query = query.Where(t => t.QueueId == filter.QueueId.Value);

        if (filter.AssignedAgentEmployeeId.HasValue)
        {
            query = query.Where(t =>
                t.AssignedAgentEmployeeId ==
                filter.AssignedAgentEmployeeId.Value);
        }

        if (filter.UnassignedOnly)
        {
            query = query.Where(t =>
                t.AssignedAgentEmployeeId == null
                && t.Status != TicketStatus.Resolved
                && t.Status != TicketStatus.Closed
                && t.Status != TicketStatus.Cancelled);
        }

        if (filter.CreatedFromUtc.HasValue)
        {
            query = query.Where(t =>
                t.CreatedAtUtc >= filter.CreatedFromUtc.Value);
        }

        if (filter.CreatedToExclusiveUtc.HasValue)
        {
            query = query.Where(t =>
                t.CreatedAtUtc < filter.CreatedToExclusiveUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search;

            query = query.Where(t =>
                t.TicketNumber.Contains(search)
                || t.Title.Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<TicketHistory>> GetHistoryAsync(
        int ticketId,
        CancellationToken ct = default)
    {
        return await _db.TicketHistories
            .AsNoTracking()
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.ChangedAtUtc)
            .ThenBy(h => h.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AgentWorkloadRow>> GetAgentWorkloadAsync(
        int? queueId,
        CancellationToken ct = default)
    {
        var query = _db.Tickets
            .AsNoTracking()
            .Where(t =>
                t.AssignedAgentEmployeeId != null
                && (t.Status == TicketStatus.Assigned
                    || t.Status == TicketStatus.InProgress
                    || t.Status == TicketStatus.WaitingForUser
                    || t.Status == TicketStatus.Reopened));

        if (queueId.HasValue)
        {
            query = query.Where(t => t.QueueId == queueId.Value);
        }

        return await query
            .GroupBy(t => t.AssignedAgentEmployeeId)
            .Select(g => new AgentWorkloadRow(
                g.Key,
                g.Count(t => t.Status == TicketStatus.Assigned),
                g.Count(t => t.Status == TicketStatus.InProgress),
                g.Count(t => t.Status == TicketStatus.WaitingForUser),
                g.Count(t => t.Status == TicketStatus.Reopened),
                g.Count(t =>
                    t.Priority == TicketPriority.High
                    || t.Priority == TicketPriority.Critical)))
            .ToListAsync(ct);
    }

    public async Task AddAsync(
        Ticket ticket,
        CancellationToken ct = default)
    {
        await _db.Tickets.AddAsync(ticket, ct);
    }
}
