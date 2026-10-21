using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Application.Common;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Tickets.Services;

public class AgentWorkloadService : IAgentWorkloadService
{
    private readonly ITicketRepository _tickets;
    private readonly IQueueRepository _queues;
    private readonly ICurrentUserService _user;

    public AgentWorkloadService(ITicketRepository tickets, IQueueRepository queues, ICurrentUserService user)
    {
        _tickets = tickets;
        _queues = queues;
        _user = user;
    }

    public async Task<IReadOnlyList<AgentWorkloadResponse>> GetAsync(int? queueId, CancellationToken ct = default)
    {
        _user.RequireManagerOrAdmin();

        if (queueId.HasValue)
        {
            var queue = await _queues.GetByIdAsync(queueId.Value, ct);
            if (queue is null) throw new NotFoundException(nameof(TicketQueue), queueId.Value);
        }

        var rows = await _tickets.GetAgentWorkloadAsync(queueId, ct);
        var memberIds = await _queues.GetActiveMemberIdsAsync(queueId, ct);

        var byAgent = rows
            .Where(r => r.AgentEmployeeId.HasValue)
            .ToDictionary(r => r.AgentEmployeeId.GetValueOrDefault());

        return byAgent.Keys
            .Union(memberIds)
            .Select(agentId => byAgent.TryGetValue(agentId, out var row)
                ? new AgentWorkloadResponse(
                    agentId, row.Assigned, row.InProgress, row.WaitingForUser, row.Reopened,
                    row.Assigned + row.InProgress + row.WaitingForUser + row.Reopened, row.HighPriority)
                : new AgentWorkloadResponse(agentId, 0, 0, 0, 0, 0, 0))
            .OrderByDescending(w => w.ActiveTotal)
            .ThenBy(w => w.AgentEmployeeId)
            .ToList();
    }
}
