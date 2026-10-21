using ServiceDesk.Application.Common;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Catalog.Dtos;

public class ServiceItemQuery : PagedQuery
{
    public int? CategoryId { get; set; }
}

/// <param name="DefaultQueueId">Routing rule: tickets for this service start in this queue (optional).</param>
public sealed record CreateServiceItemRequest(
    string Name, string? Description, int CategoryId, TicketPriority DefaultPriority, int? DefaultQueueId);

public sealed record UpdateServiceItemRequest(
    string Name, string? Description, int CategoryId, TicketPriority DefaultPriority, int? DefaultQueueId);

public sealed record ServiceItemResponse(
    int Id, string Name, string? Description, int CategoryId, string CategoryName,
    TicketPriority DefaultPriority, int? DefaultQueueId, bool IsActive,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
