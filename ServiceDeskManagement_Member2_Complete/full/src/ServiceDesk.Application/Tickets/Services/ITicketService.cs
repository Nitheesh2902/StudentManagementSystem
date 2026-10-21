using ServiceDesk.Application.Common;
using ServiceDesk.Application.Tickets.Dtos;

namespace ServiceDesk.Application.Tickets.Services;

public interface ITicketService
{
    Task<PagedResult<TicketSummaryResponse>> GetPagedAsync(TicketQuery query, CancellationToken ct = default);
    Task<PagedResult<TicketSummaryResponse>> GetUnassignedAsync(TicketQuery query, CancellationToken ct = default);
    Task<TicketResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct = default);
    Task<TicketResponse> UpdateAsync(int id, UpdateTicketRequest request, CancellationToken ct = default);
    Task<TicketResponse> ChangeStatusAsync(int id, ChangeStatusRequest request, CancellationToken ct = default);
    Task<TicketResponse> ReopenAsync(int id, ReopenTicketRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TicketHistoryResponse>> GetHistoryAsync(int id, CancellationToken ct = default);
}
