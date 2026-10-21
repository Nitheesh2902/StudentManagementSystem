using ServiceDesk.Application.Queues.Dtos;

namespace ServiceDesk.Application.Queues.Services;

public interface IQueueService
{
    Task<IReadOnlyList<QueueResponse>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task<QueueResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<QueueResponse> CreateAsync(CreateQueueRequest request, CancellationToken ct = default);
    Task<QueueResponse> UpdateAsync(int id, UpdateQueueRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
    Task<QueueResponse> AddMemberAsync(int queueId, AddQueueMemberRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(int queueId, int agentEmployeeId, CancellationToken ct = default);
}
