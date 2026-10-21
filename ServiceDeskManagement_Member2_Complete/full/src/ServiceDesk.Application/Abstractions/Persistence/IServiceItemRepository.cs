using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Abstractions.Persistence;

public interface IServiceItemRepository
{
    /// <summary>Tracked entity including its Category, for updates.</summary>
    Task<ServiceItem?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(IReadOnlyList<ServiceItem> Items, int TotalCount)> GetPagedAsync(
        int? categoryId, string? search, bool includeInactive, int page, int pageSize, CancellationToken ct = default);

    Task<bool> NameExistsAsync(string name, int categoryId, int? excludeId, CancellationToken ct = default);
    Task AddAsync(ServiceItem service, CancellationToken ct = default);
}
