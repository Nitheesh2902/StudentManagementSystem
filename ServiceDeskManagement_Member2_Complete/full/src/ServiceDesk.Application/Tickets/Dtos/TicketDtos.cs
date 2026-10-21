using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Tickets.Dtos;

public sealed record CreateTicketRequest(
    string Title, string Description, int ServiceItemId, TicketPriority? Priority, int? AssetId);

public sealed record UpdateTicketRequest(string Title, string Description, TicketPriority? Priority);

/// <summary>Note is required when resolving; optional otherwise (e.g. a cancel reason).</summary>
public sealed record ChangeStatusRequest(TicketStatus NewStatus, string? Note);

public sealed record ReopenTicketRequest(string Reason);

public class TicketQuery
{
    /// <summary>Matches ticket number or title.</summary>
    public string? Search { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public int? CategoryId { get; set; }
    public int? ServiceItemId { get; set; }
    public int? QueueId { get; set; }
    /// <summary>Only tickets assigned to the signed-in agent.</summary>
    public bool AssignedToMe { get; set; }
    /// <summary>Only honoured for service desk staff; employees always see their own tickets.</summary>
    public int? RequesterEmployeeId { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <param name="AllowedNextStatuses">Statuses the CURRENT user may move this ticket to (drives the UI buttons).</param>
/// <param name="CanReopen">True when the ticket is resolved and still inside the reopen window.</param>
public sealed record TicketResponse(
    int Id, string TicketNumber, string Title, string Description, int RequesterEmployeeId,
    int CategoryId, string CategoryName, int ServiceItemId, string ServiceName,
    int QueueId, string QueueName, int? AssignedAgentEmployeeId, bool IsEscalated,
    TicketPriority Priority, TicketStatus Status,
    DateTime? DueResponseAtUtc, DateTime? DueResolutionAtUtc, DateTime? ResolvedAtUtc, DateTime? ClosedAtUtc,
    string? ResolutionNote, int ReopenCount,
    int? AssetId, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc,
    IReadOnlyList<TicketStatus> AllowedNextStatuses, bool CanReopen);

public sealed record TicketSummaryResponse(
    int Id, string TicketNumber, string Title, int RequesterEmployeeId,
    string CategoryName, string ServiceName, string QueueName, int? AssignedAgentEmployeeId,
    TicketPriority Priority, TicketStatus Status, bool IsEscalated,
    DateTime? DueResolutionAtUtc, DateTime CreatedAtUtc);

public sealed record TicketHistoryResponse(
    int Id, TicketHistoryAction Action, string? FieldName, string? OldValue, string? NewValue, string? Note,
    int? ChangedByUserId, DateTime ChangedAtUtc);
