using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Application.Tickets.Services;

namespace ServiceDesk.API.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:int}/comments")]
[Produces("application/json")]
public class TicketCommentsController : ControllerBase
{
    private readonly ITicketCommentService _service;

    public TicketCommentsController(ITicketCommentService service) => _service = service;

    /// <summary>Comments of a ticket. Internal notes are only returned to service desk staff.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommentResponse>>> GetAll(int ticketId, CancellationToken ct)
        => Ok(await _service.GetAsync(ticketId, ct));

    [HttpPost]
    public async Task<ActionResult<CommentResponse>> Add(int ticketId, CreateCommentRequest request, CancellationToken ct)
    {
        var created = await _service.AddAsync(ticketId, request, ct);
        return CreatedAtAction(nameof(GetAll), new { ticketId }, created);
    }
}
