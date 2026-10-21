
using EmployeeServiceDesk.Application.Abstractions.Persistence;
using EmployeeServiceDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Domain.Entities;

namespace EmployeeServiceDesk.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<TicketCategory?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return _db.TicketCategories
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<(IReadOnlyList<TicketCategory> Items, int TotalCount)>
        GetPagedAsync(
            string? search,
            bool includeInactive,
            int page,
            int pageSize,
            CancellationToken ct = default)
    {
        var query = _db.TicketCategories
            .AsNoTracking()
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.Contains(search));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<bool> NameExistsAsync(
        string name,
        int? excludeId,
        CancellationToken ct = default)
    {
        return _db.TicketCategories.AnyAsync(
            c => c.Name == name &&
                 (excludeId == null || c.Id != excludeId),
            ct);
    }

    public Task<bool> HasActiveServicesAsync(
        int categoryId,
        CancellationToken ct = default)
    {
        return _db.ServiceItems.AnyAsync(
            s => s.CategoryId == categoryId && s.IsActive,
            ct);
    }

    public async Task AddAsync(
        TicketCategory category,
        CancellationToken ct = default)
    {
        await _db.TicketCategories.AddAsync(category, ct);
    }
}
