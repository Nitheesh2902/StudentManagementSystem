namespace ServiceDesk.Application.Tickets.Dtos;

/// <param name="IsInternal">Internal notes are visible to service desk staff only.</param>
public sealed record CreateCommentRequest(string Body, bool IsInternal);

public sealed record CommentResponse(
    int Id, int TicketId, int AuthorEmployeeId, string Body, bool IsInternal, DateTime CreatedAtUtc);
