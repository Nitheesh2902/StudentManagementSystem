using ServiceDesk.Application.Catalog.Dtos;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Catalog;

internal static class CatalogMappings
{
    public static CategoryResponse ToResponse(this TicketCategory c) =>
        new(c.Id, c.Name, c.Description, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc);

    public static ServiceItemResponse ToResponse(this ServiceItem s) =>
        new(s.Id, s.Name, s.Description, s.CategoryId, s.Category.Name,
            s.DefaultPriority, s.DefaultQueueId, s.IsActive, s.CreatedAtUtc, s.UpdatedAtUtc);
}
