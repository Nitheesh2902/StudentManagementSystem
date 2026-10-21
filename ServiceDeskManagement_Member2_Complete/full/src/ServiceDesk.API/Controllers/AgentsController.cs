using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Application.Tickets.Services;

namespace ServiceDesk.API.Controllers;

[ApiController]
[Route("api/agents")]
[Produces("application/json")]
public class AgentsController : ControllerBase
{
    private readonly IAgentWorkloadService _service;

    public AgentsController(IAgentWorkloadService service) => _service = service;

    /// <summary>Open tickets per agent (managers and administrators). Optionally limited to one queue.</summary>
    [HttpGet("workload")]
    public async Task<ActionResult<IReadOnlyList<AgentWorkloadResponse>>> GetWorkload(
        [FromQuery] int? queueId, CancellationToken ct)
        => Ok(await _service.GetAsync(queueId, ct));
}
