using ServiceDesk.Application.Tickets.Dtos;

namespace ServiceDesk.Application.Tickets.Services;

public interface IAgentWorkloadService
{
    /// <summary>Managers and administrators only. Queue members with no tickets are listed with zeros.</summary>
    Task<IReadOnlyList<AgentWorkloadResponse>> GetAsync(int? queueId, CancellationToken ct = default);
}
