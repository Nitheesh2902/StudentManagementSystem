using EmployeeServiceDesk.Application.Abstractions.Persistence;
using FluentValidation;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Application.Common;
using ServiceDesk.Application.Queues.Dtos;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Queues.Services;

public class QueueService : IQueueService
{
    private readonly IQueueRepository _queues;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;
    private readonly IValidator<CreateQueueRequest> _createValidator;
    private readonly IValidator<UpdateQueueRequest> _updateValidator;
    private readonly IValidator<AddQueueMemberRequest> _memberValidator;

    public QueueService(
        IQueueRepository queues,
        IUnitOfWork uow,
        IAuditService audit,
        ICurrentUserService user,
        IValidator<CreateQueueRequest> createValidator,
        IValidator<UpdateQueueRequest> updateValidator,
        IValidator<AddQueueMemberRequest> memberValidator)
    {
        _queues = queues;
        _uow = uow;
        _audit = audit;
        _user = user;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _memberValidator = memberValidator;
    }

    public async Task<IReadOnlyList<QueueResponse>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        _user.RequireStaff();

        var withInactive = includeInactive && _user.IsManagerOrAdmin();
        var queues = await _queues.GetAllAsync(withInactive, ct);
        return queues.Select(q => q.ToResponse()).ToList();
    }

    public async Task<QueueResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _user.RequireStaff();
        return (await GetOrThrowAsync(id, ct)).ToResponse();
    }

    public async Task<QueueResponse> CreateAsync(CreateQueueRequest request, CancellationToken ct = default)
    {
        _user.RequireManagerOrAdmin();
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var name = request.Name.Trim();
        if (await _queues.NameExistsAsync(name, null, ct))
            throw new ConflictException($"A queue named '{name}' already exists.");

        var queue = new TicketQueue(name, request.Description, request.DepartmentId, request.ManagerEmployeeId);
        await _queues.AddAsync(queue, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("Create", nameof(TicketQueue), queue.Id.ToString(), $"Name={queue.Name}", ct);
        return queue.ToResponse();
    }

    public async Task<QueueResponse> UpdateAsync(int id, UpdateQueueRequest request, CancellationToken ct = default)
    {
        _user.RequireManagerOrAdmin();
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var queue = await GetOrThrowAsync(id, ct);
        var name = request.Name.Trim();
        if (await _queues.NameExistsAsync(name, id, ct))
            throw new ConflictException($"A queue named '{name}' already exists.");

        queue.Update(name, request.Description, request.DepartmentId, request.ManagerEmployeeId);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("Update", nameof(TicketQueue), id.ToString(), $"Name={queue.Name}", ct);
        return queue.ToResponse();
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        _user.RequireManagerOrAdmin();
        var queue = await GetOrThrowAsync(id, ct);
        if (!queue.IsActive) return;

        if (await _queues.HasActiveTicketsAsync(id, ct))
            throw new ConflictException(
                "This queue still holds open tickets. Move or finish them before deactivating the queue.");

        queue.Deactivate(); // the general queue refuses this
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("Deactivate", nameof(TicketQueue), id.ToString(), null, ct);
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        _user.RequireManagerOrAdmin();
        var queue = await GetOrThrowAsync(id, ct);
        if (queue.IsActive) return;

        queue.Activate();
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("Activate", nameof(TicketQueue), id.ToString(), null, ct);
    }

    public async Task<QueueResponse> AddMemberAsync(int queueId, AddQueueMemberRequest request, CancellationToken ct = default)
    {
        _user.RequireManagerOrAdmin();
        await _memberValidator.ValidateAndThrowAsync(request, ct);

        var queue = await GetOrThrowAsync(queueId, ct);
        if (!queue.IsActive)
            throw new DomainException("Members cannot be added to an inactive queue.");

        queue.AddMember(request.AgentEmployeeId);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("AddMember", nameof(TicketQueue), queueId.ToString(),
            $"Agent={request.AgentEmployeeId}", ct);
        return queue.ToResponse();
    }

    public async Task RemoveMemberAsync(int queueId, int agentEmployeeId, CancellationToken ct = default)
    {
        _user.RequireManagerOrAdmin();

        var queue = await GetOrThrowAsync(queueId, ct);
        queue.RemoveMember(agentEmployeeId);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("RemoveMember", nameof(TicketQueue), queueId.ToString(),
            $"Agent={agentEmployeeId}", ct);
    }

    private async Task<TicketQueue> GetOrThrowAsync(int id, CancellationToken ct)
        => await _queues.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(TicketQueue), id);
}
