using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Tickets;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Application.Tickets.Services;

namespace ServiceDesk.API.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:int}/attachments")]
public class TicketAttachmentsController : ControllerBase
{
    private readonly ITicketAttachmentService _service;

    public TicketAttachmentsController(ITicketAttachmentService service) => _service = service;

    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<AttachmentResponse>>> GetAll(int ticketId, CancellationToken ct)
        => Ok(await _service.ListAsync(ticketId, ct));

    /// <summary>Upload one file (multipart/form-data, field name "file"). Whitelisted types only, max 5 MB.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [RequestSizeLimit(AttachmentRules.MaxBytes + 512 * 1024)]
    public async Task<ActionResult<AttachmentResponse>> Upload(int ticketId, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var created = await _service.UploadAsync(ticketId, file.FileName, stream, ct);
        return CreatedAtAction(nameof(GetAll), new { ticketId }, created);
    }

    [HttpGet("{attachmentId:int}/download")]
    public async Task<IActionResult> Download(int ticketId, int attachmentId, CancellationToken ct)
    {
        var download = await _service.DownloadAsync(ticketId, attachmentId, ct);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(download.Content, download.ContentType, download.FileName);
    }

    [HttpDelete("{attachmentId:int}")]
    public async Task<IActionResult> Delete(int ticketId, int attachmentId, CancellationToken ct)
    {
        await _service.DeleteAsync(ticketId, attachmentId, ct);
        return NoContent();
    }
}
