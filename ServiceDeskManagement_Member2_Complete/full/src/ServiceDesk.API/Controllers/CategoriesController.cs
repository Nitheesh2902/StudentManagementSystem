using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Catalog.Dtos;
using ServiceDesk.Application.Catalog.Services;
using ServiceDesk.Application.Common;

namespace ServiceDesk.API.Controllers;

/// <summary>Ticket categories. Reading needs a signed-in user; changes need an administrator (checked in the service).</summary>
[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryResponse>>> GetAll(
        [FromQuery] CategoryQuery query, CancellationToken ct)
        => Ok(await _service.GetPagedAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Update(int id, UpdateCategoryRequest request, CancellationToken ct)
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
