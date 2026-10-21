using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Application.Tickets.Services;

namespace ServiceDesk.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _service;

    public TicketsController(ITicketService service) => _service = service;

    /// <summary>
    /// Search / filter tickets (status, priority, category, service, queue, date range).
    /// Employees see their own tickets, agents the tickets of their queues, managers everything.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketSummaryResponse>>> GetAll(
        [FromQuery] TicketQuery query, CancellationToken ct)
        => Ok(await _service.GetPagedAsync(query, ct));

    /// <summary>Open tickets nobody is working on yet (staff only).</summary>
    [HttpGet("unassigned")]
    public async Task<ActionResult<PagedResult<TicketSummaryResponse>>> GetUnassigned(
        [FromQuery] TicketQuery query, CancellationToken ct)
        => Ok(await _service.GetUnassignedAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketResponse>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    /// <summary>Create a ticket. The requester is taken from the signed-in user, never from the body.</summary>
    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Create(CreateTicketRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TicketResponse>> Update(int id, UpdateTicketRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    /// <summary>Move the ticket along its lifecycle. A note is required when resolving.</summary>
    [HttpPost("{id:int}/status")]
    public async Task<ActionResult<TicketResponse>> ChangeStatus(int id, ChangeStatusRequest request, CancellationToken ct)
        => Ok(await _service.ChangeStatusAsync(id, request, ct));

    /// <summary>Reopen a resolved ticket (only inside the reopen window).</summary>
    [HttpPost("{id:int}/reopen")]
    public async Task<ActionResult<TicketResponse>> Reopen(int id, ReopenTicketRequest request, CancellationToken ct)
        => Ok(await _service.ReopenAsync(id, request, ct));

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyList<TicketHistoryResponse>>> GetHistory(int id, CancellationToken ct)
        => Ok(await _service.GetHistoryAsync(id, ct));
}
