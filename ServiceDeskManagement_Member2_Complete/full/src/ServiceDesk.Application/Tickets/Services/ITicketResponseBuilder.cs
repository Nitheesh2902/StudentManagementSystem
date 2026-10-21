using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Application.Tickets.Services;

/// <summary>Builds the detailed ticket response, including what the CURRENT user may do next.</summary>
public interface ITicketResponseBuilder
{
    TicketResponse Build(Ticket ticket);
}
