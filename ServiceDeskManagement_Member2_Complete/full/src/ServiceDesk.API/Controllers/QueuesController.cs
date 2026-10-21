using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Queues.Dtos;
using ServiceDesk.Application.Queues.Services;

namespace ServiceDesk.API.Controllers;

/// <summary>Team queues and their agents. Staff can read; managers and administrators can change.</summary>
[ApiController]
[Route("api/queues")]
[Produces("application/json")]
public class QueuesController : ControllerBase
{
    private readonly IQueueService _service;

    public QueuesController(IQueueService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QueueResponse>>> GetAll(
        [FromQuery] bool includeInactive, CancellationToken ct)
        => Ok(await _service.GetAllAsync(includeInactive, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QueueResponse>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<QueueResponse>> Create(CreateQueueRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<QueueResponse>> Update(int id, UpdateQueueRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _service.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        await _service.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>Add an agent (employee id) to the queue.</summary>
    [HttpPost("{id:int}/members")]
    public async Task<ActionResult<QueueResponse>> AddMember(int id, AddQueueMemberRequest request, CancellationToken ct)
        => Ok(await _service.AddMemberAsync(id, request, ct));

    [HttpDelete("{id:int}/members/{agentEmployeeId:int}")]
    public async Task<IActionResult> RemoveMember(int id, int agentEmployeeId, CancellationToken ct)
    {
        await _service.RemoveMemberAsync(id, agentEmployeeId, ct);
        return NoContent();
    }
}
