using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Tickets;

internal static class TicketMappings
{
    public static TicketSummaryResponse ToSummary(this Ticket t) =>
        new(t.Id, t.TicketNumber, t.Title, t.RequesterEmployeeId,
            t.Category.Name, t.ServiceItem.Name, t.Queue.Name, t.AssignedAgentEmployeeId,
            t.Priority, t.Status, t.IsEscalated, t.DueResolutionAtUtc, t.CreatedAtUtc);

    public static TicketHistoryResponse ToResponse(this TicketHistory h) =>
        new(h.Id, h.Action, h.FieldName, h.OldValue, h.NewValue, h.Note, h.ChangedByUserId, h.ChangedAtUtc);

    public static CommentResponse ToResponse(this TicketComment c) =>
        new(c.Id, c.TicketId, c.AuthorEmployeeId, c.Body, c.IsInternal, c.CreatedAtUtc);

    public static AttachmentResponse ToResponse(this TicketAttachment a) =>
        new(a.Id, a.TicketId, a.FileName, a.ContentType, a.SizeBytes, a.UploadedByEmployeeId, a.CreatedAtUtc);
}
