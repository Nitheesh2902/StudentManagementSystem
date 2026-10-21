using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.Entities;

public class TicketComment : BaseEntity
{
    private TicketComment() { } // EF Core

    public TicketComment(int authorEmployeeId, string body, bool isInternal)
    {
        if (authorEmployeeId <= 0) throw new DomainException("Author is required.");
        if (string.IsNullOrWhiteSpace(body)) throw new DomainException("Comment text is required.");

        AuthorEmployeeId = authorEmployeeId;
        Body = body.Trim();
        IsInternal = isInternal;
    }

    public int TicketId { get; private set; }
    public int AuthorEmployeeId { get; private set; }
    public string Body { get; private set; } = default!;

    /// <summary>Internal notes are visible to service desk staff only.</summary>
    public bool IsInternal { get; private set; }
}
