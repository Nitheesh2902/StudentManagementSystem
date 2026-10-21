using ServiceDesk.Application.Queues.Dtos;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Queues;

internal static class QueueMappings
{
    public static QueueResponse ToResponse(this TicketQueue q) =>
        new(q.Id, q.Name, q.Description, q.DepartmentId, q.ManagerEmployeeId, q.IsActive,
            q.ActiveMemberIds, q.CreatedAtUtc, q.UpdatedAtUtc);
}
