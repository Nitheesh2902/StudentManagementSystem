using EmployeeServiceDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public class ServiceItemRepository : IServiceItemRepository
{
    private readonly AppDbContext _db;

    public ServiceItemRepository(AppDbContext db) => _db = db;

    public Task<ServiceItem?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.ServiceItems.Include(s => s.Category).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<(IReadOnlyList<ServiceItem> Items, int TotalCount)> GetPagedAsync(
        int? categoryId, string? search, bool includeInactive, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ServiceItems.AsNoTracking().Include(s => s.Category).AsQueryable();

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.Category.Name).ThenBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<bool> NameExistsAsync(string name, int categoryId, int? excludeId, CancellationToken ct = default)
        => _db.ServiceItems.AnyAsync(
            s => s.CategoryId == categoryId && s.Name == name && (excludeId == null || s.Id != excludeId), ct);

    public async Task AddAsync(ServiceItem service, CancellationToken ct = default)
        => await _db.ServiceItems.AddAsync(service, ct);
}
