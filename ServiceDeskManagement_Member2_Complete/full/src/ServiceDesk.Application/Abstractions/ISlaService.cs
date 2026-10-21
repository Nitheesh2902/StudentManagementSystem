using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Abstractions;

public sealed record SlaDeadlines(DateTime? ResponseDueUtc, DateTime? ResolutionDueUtc);

/// <summary>Owner: Member 3. Used by tickets to get deadlines at creation / priority change.</summary>
public interface ISlaService
{
    Task<SlaDeadlines> CalculateDeadlinesAsync(
        TicketPriority priority, int serviceItemId, DateTime createdAtUtc, CancellationToken ct = default);
}
