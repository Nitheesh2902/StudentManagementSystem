using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Catalog.Dtos;
using ServiceDesk.Application.Catalog.Services;
using ServiceDesk.Application.Common;

namespace ServiceDesk.API.Controllers;

/// <summary>The service catalog. Reading needs a signed-in user; changes need an administrator (checked in the service).</summary>
[ApiController]
[Route("api/services")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly IServiceItemService _service;

    public ServicesController(IServiceItemService service) => _service = service;

    /// <summary>List catalog services (paged; filter by category, search by name).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceItemResponse>>> GetAll(
        [FromQuery] ServiceItemQuery query, CancellationToken ct)
        => Ok(await _service.GetPagedAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceItemResponse>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ServiceItemResponse>> Create(CreateServiceItemRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceItemResponse>> Update(int id, UpdateServiceItemRequest request, CancellationToken ct)
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
}
