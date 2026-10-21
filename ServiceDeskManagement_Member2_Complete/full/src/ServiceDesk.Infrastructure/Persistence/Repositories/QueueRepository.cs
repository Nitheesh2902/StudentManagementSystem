using EmployeeServiceDesk.Application.Abstractions.Persistence;

using EmployeeServiceDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;

namespace EmployeeServiceDesk.Infrastructure.Persistence.Repositories;

public class QueueRepository : IQueueRepository
{
    private readonly AppDbContext _db;

    public QueueRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<TicketQueue?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return _db.TicketQueues
            .Include(q => q.Members)
            .FirstOrDefaultAsync(q => q.Id == id, ct);
    }

    public async Task<IReadOnlyList<TicketQueue>> GetAllAsync(
        bool includeInactive,
        CancellationToken ct = default)
    {
        var query = _db.TicketQueues
            .AsNoTracking()
            .Include(q => q.Members.Where(m => m.IsActive))
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(q => q.IsActive);
        }

        return await query
            .OrderBy(q => q.Name)
            .ToListAsync(ct);
    }

    public Task<bool> NameExistsAsync(
        string name,
        int? excludeId,
        CancellationToken ct = default)
    {
        return _db.TicketQueues.AnyAsync(
            q => q.Name == name &&
                 (excludeId == null || q.Id != excludeId),
            ct);
    }

    public Task<bool> IsActiveMemberAsync(
        int queueId,
        int employeeId,
        CancellationToken ct = default)
    {
        return _db.TicketQueueMembers.AnyAsync(
            m => m.QueueId == queueId &&
                 m.AgentEmployeeId == employeeId &&
                 m.IsActive,
            ct);
    }

    public async Task<IReadOnlyList<int>> GetActiveMemberIdsAsync(
        int? queueId,
        CancellationToken ct = default)
    {
        var query = _db.TicketQueueMembers
            .AsNoTracking()
            .Where(m => m.IsActive);

        if (queueId.HasValue)
        {
            query = query.Where(m => m.QueueId == queueId.Value);
        }

        return await query
            .Select(m => m.AgentEmployeeId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(ct);
    }

    public Task<bool> HasActiveTicketsAsync(
        int queueId,
        CancellationToken ct = default)
    {
        return _db.Tickets.AnyAsync(
            t => t.QueueId == queueId &&
                 t.Status != TicketStatus.Resolved &&
                 t.Status != TicketStatus.Closed &&
                 t.Status != TicketStatus.Cancelled,
            ct);
    }

    public async Task AddAsync(
        TicketQueue queue,
        CancellationToken ct = default)
    {
        await _db.TicketQueues.AddAsync(queue, ct);
    }
}
