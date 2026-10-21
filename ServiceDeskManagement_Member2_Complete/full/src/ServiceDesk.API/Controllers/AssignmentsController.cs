using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Application.Tickets.Services;

namespace ServiceDesk.API.Controllers;

/// <summary>Assign, reassign and escalate tickets (service desk staff).</summary>
[ApiController]
[Route("api/tickets/{ticketId:int}")]
[Produces("application/json")]
public class AssignmentsController : ControllerBase
{
    private readonly ITicketAssignmentService _service;

    public AssignmentsController(ITicketAssignmentService service) => _service = service;

    /// <summary>First assignment. Agents can take a ticket for themselves; managers can assign anyone in the queue.</summary>
    [HttpPost("assign")]
    public async Task<ActionResult<TicketResponse>> Assign(int ticketId, AssignTicketRequest request, CancellationToken ct)
        => Ok(await _service.AssignAsync(ticketId, request, ct));

    /// <summary>Hand an assigned ticket to another agent of the queue.</summary>
    [HttpPost("reassign")]
    public async Task<ActionResult<TicketResponse>> Reassign(int ticketId, ReassignTicketRequest request, CancellationToken ct)
        => Ok(await _service.ReassignAsync(ticketId, request, ct));

    /// <summary>Escalate to another team queue OR to a manager (give exactly one of the two).</summary>
    [HttpPost("escalate")]
    public async Task<ActionResult<TicketResponse>> Escalate(int ticketId, EscalateTicketRequest request, CancellationToken ct)
        => Ok(await _service.EscalateAsync(ticketId, request, ct));
}
