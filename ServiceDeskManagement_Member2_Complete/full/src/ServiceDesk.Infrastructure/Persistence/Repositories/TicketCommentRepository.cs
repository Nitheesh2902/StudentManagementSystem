
using EmployeeServiceDesk.Application.Abstractions.Persistence;
using EmployeeServiceDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Domain.Entities;

namespace EmployeeServiceDesk.Infrastructure.Persistence.Repositories;

public class TicketCommentRepository : ITicketCommentRepository
{
    private readonly AppDbContext _db;

    public TicketCommentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TicketComment>> GetByTicketAsync(
        int ticketId,
        bool includeInternal,
        CancellationToken ct = default)
    {
        return await _db.TicketComments
            .AsNoTracking()
            .Where(c => c.TicketId == ticketId &&
                        (includeInternal || !c.IsInternal))
            .OrderBy(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(ct);
    }
}
