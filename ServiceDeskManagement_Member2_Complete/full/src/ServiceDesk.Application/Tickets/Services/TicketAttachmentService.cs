using EmployeeServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Application.Common;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Tickets.Services;

public class TicketAttachmentService : ITicketAttachmentService
{
    private readonly ITicketAttachmentRepository _attachments;
    private readonly ITicketAccessService _access;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _user;
    private readonly IAuditService _audit;

    public TicketAttachmentService(
        ITicketAttachmentRepository attachments,
        ITicketAccessService access,
        IFileStorage storage,
        IUnitOfWork uow,
        ICurrentUserService user,
        IAuditService audit)
    {
        _attachments = attachments;
        _access = access;
        _storage = storage;
        _uow = uow;
        _user = user;
        _audit = audit;
    }

    public async Task<IReadOnlyList<AttachmentResponse>> ListAsync(int ticketId, CancellationToken ct = default)
    {
        await _access.GetAccessibleAsync(ticketId, ct);
        var items = await _attachments.GetByTicketAsync(ticketId, ct);
        return items.Select(a => a.ToResponse()).ToList();
    }

    public async Task<AttachmentResponse> UploadAsync(
        int ticketId, string fileName, Stream content, CancellationToken ct = default)
    {
        var ticket = await _access.GetAccessibleAsync(ticketId, ct);
        var me = _user.RequireEmployeeId();

        var safeName = AttachmentRules.SanitizeFileName(fileName);
        var extension = Path.GetExtension(safeName);
        if (string.IsNullOrWhiteSpace(safeName) || !AttachmentRules.TryGetContentType(extension, out var contentType))
            throw new DomainException(
                $"This file type is not allowed. Allowed types: {string.Join(", ", AttachmentRules.AllowedExtensions)}.");

        if (await _attachments.CountActiveAsync(ticketId, ct) >= AttachmentRules.MaxPerTicket)
            throw new DomainException($"A ticket can have at most {AttachmentRules.MaxPerTicket} attachments.");

        // Read into memory with a hard size cap (never trust the declared length).
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        long total = 0;
        int read;
        while ((read = await content.ReadAsync(chunk.AsMemory(), ct)) > 0)
        {
            total += read;
            if (total > AttachmentRules.MaxBytes)
                throw new DomainException(
                    $"The file is too large. The maximum size is {AttachmentRules.MaxBytes / (1024 * 1024)} MB.");
            buffer.Write(chunk, 0, read);
        }

        if (total == 0)
            throw new DomainException("The file is empty.");

        if (!HasValidSignature(buffer, extension, total))
            throw new DomainException("The file content does not match its extension.");

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        buffer.Position = 0;
        await _storage.SaveAsync(storedName, buffer, ct);

        var attachment = ticket.AddAttachment(safeName, storedName, contentType, total, me, _user.UserId);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("AddAttachment", nameof(Ticket), ticketId.ToString(), safeName, ct);
        return attachment.ToResponse();
    }

    public async Task<AttachmentDownload> DownloadAsync(int ticketId, int attachmentId, CancellationToken ct = default)
    {
        await _access.GetAccessibleAsync(ticketId, ct);

        var attachment = await _attachments.GetByIdAsync(ticketId, attachmentId, ct)
                         ?? throw new NotFoundException(nameof(TicketAttachment), attachmentId);

        var stream = await _storage.OpenReadAsync(attachment.StoredFileName, ct);
        return new AttachmentDownload(stream, attachment.FileName, attachment.ContentType);
    }

    public async Task DeleteAsync(int ticketId, int attachmentId, CancellationToken ct = default)
    {
        var ticket = await _access.GetAccessibleAsync(ticketId, ct);
        var me = _user.RequireEmployeeId();

        var attachment = await _attachments.GetByIdAsync(ticketId, attachmentId, ct)
                         ?? throw new NotFoundException(nameof(TicketAttachment), attachmentId);

        if (attachment.UploadedByEmployeeId != me && !_user.IsManagerOrAdmin())
            throw new ForbiddenException("Only the person who uploaded the file, or a manager, can remove it.");

        // Soft delete: the record (and file) stay for traceability, but the attachment disappears from the ticket.
        ticket.RemoveAttachment(attachment, _user.UserId);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("RemoveAttachment", nameof(Ticket), ticketId.ToString(), attachment.FileName, ct);
    }

    // Kept synchronous: Span<T> locals are not allowed in async methods on older C# versions.
    private static bool HasValidSignature(MemoryStream buffer, string extension, long total)
    {
        var length = (int)Math.Min(total, 16);
        return AttachmentRules.SignatureMatches(extension, buffer.GetBuffer().AsSpan(0, length));
    }
}
