
using EmployeeServiceDesk.Application.Abstractions.Persistence;
using EmployeeServiceDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Domain.Entities;

namespace EmployeeServiceDesk.Infrastructure.Persistence.Repositories;

public class TicketAttachmentRepository : ITicketAttachmentRepository
{
    private readonly AppDbContext _db;

    public TicketAttachmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TicketAttachment>> GetByTicketAsync(
        int ticketId,
        CancellationToken ct = default)
    {
        return await _db.TicketAttachments
            .AsNoTracking()
            .Where(a => a.TicketId == ticketId && a.IsActive)
            .OrderBy(a => a.CreatedAtUtc)
            .ThenBy(a => a.Id)
            .ToListAsync(ct);
    }

    public Task<TicketAttachment?> GetByIdAsync(
        int ticketId,
        int attachmentId,
        CancellationToken ct = default)
    {
        return _db.TicketAttachments.FirstOrDefaultAsync(
            a => a.Id == attachmentId &&
                 a.TicketId == ticketId &&
                 a.IsActive,
            ct);
    }

    public Task<int> CountActiveAsync(
        int ticketId,
        CancellationToken ct = default)
    {
        return _db.TicketAttachments.CountAsync(
            a => a.TicketId == ticketId && a.IsActive,
            ct);
    }
}
