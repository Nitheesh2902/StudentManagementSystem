using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Abstractions.Persistence;

public interface ICategoryRepository
{
    /// <summary>Tracked entity, for updates.</summary>
    Task<TicketCategory?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(IReadOnlyList<TicketCategory> Items, int TotalCount)> GetPagedAsync(
        string? search, bool includeInactive, int page, int pageSize, CancellationToken ct = default);

    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
    Task<bool> HasActiveServicesAsync(int categoryId, CancellationToken ct = default);
    Task AddAsync(TicketCategory category, CancellationToken ct = default);
}
