using ServiceDesk.Application.Tickets.Dtos;

namespace ServiceDesk.Application.Tickets.Services;

public interface ITicketCommentService
{
    /// <summary>Employees never receive internal notes.</summary>
    Task<IReadOnlyList<CommentResponse>> GetAsync(int ticketId, CancellationToken ct = default);
    Task<CommentResponse> AddAsync(int ticketId, CreateCommentRequest request, CancellationToken ct = default);
}
