using ServiceDesk.Application.Tickets.Dtos;

namespace ServiceDesk.Application.Tickets.Services;

public interface ITicketAssignmentService
{
    Task<TicketResponse> AssignAsync(int ticketId, AssignTicketRequest request, CancellationToken ct = default);
    Task<TicketResponse> ReassignAsync(int ticketId, ReassignTicketRequest request, CancellationToken ct = default);
    Task<TicketResponse> EscalateAsync(int ticketId, EscalateTicketRequest request, CancellationToken ct = default);
}
