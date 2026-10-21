using EmployeeServiceDesk.Application.Abstractions.Persistence;
using FluentValidation;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Application.Common;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;
using ServiceDesk.Domain.Workflow;

namespace ServiceDesk.Application.Tickets.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _tickets;
    private readonly IServiceItemRepository _services;
    private readonly IQueueRepository _queues;
    private readonly ITicketNumberGenerator _numbers;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _user;
    private readonly ISlaService _sla;
    private readonly INotificationService _notifications;
    private readonly IAssetLinkService _assets;
    private readonly IAuditService _audit;
    private readonly ITicketPolicy _policy;
    private readonly ITicketAccessService _access;
    private readonly ITicketResponseBuilder _builder;
    private readonly IValidator<CreateTicketRequest> _createValidator;
    private readonly IValidator<UpdateTicketRequest> _updateValidator;
    private readonly IValidator<ChangeStatusRequest> _statusValidator;
    private readonly IValidator<ReopenTicketRequest> _reopenValidator;
    private readonly IValidator<TicketQuery> _queryValidator;

    public TicketService(
        ITicketRepository tickets,
        IServiceItemRepository services,
        IQueueRepository queues,
        ITicketNumberGenerator numbers,
        IUnitOfWork uow,
        ICurrentUserService user,
        ISlaService sla,
        INotificationService notifications,
        IAssetLinkService assets,
        IAuditService audit,
        ITicketPolicy policy,
        ITicketAccessService access,
        ITicketResponseBuilder builder,
        IValidator<CreateTicketRequest> createValidator,
        IValidator<UpdateTicketRequest> updateValidator,
        IValidator<ChangeStatusRequest> statusValidator,
        IValidator<ReopenTicketRequest> reopenValidator,
        IValidator<TicketQuery> queryValidator)
    {
        _tickets = tickets;
        _services = services;
        _queues = queues;
        _numbers = numbers;
        _uow = uow;
        _user = user;
        _sla = sla;
        _notifications = notifications;
        _assets = assets;
        _audit = audit;
        _policy = policy;
        _access = access;
        _builder = builder;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
        _reopenValidator = reopenValidator;
        _queryValidator = queryValidator;
    }

    // ------------------------------------------------------------------ queries

    public Task<PagedResult<TicketSummaryResponse>> GetPagedAsync(TicketQuery query, CancellationToken ct = default)
        => QueryAsync(query, unassignedOnly: false, ct);

    public async Task<PagedResult<TicketSummaryResponse>> GetUnassignedAsync(TicketQuery query, CancellationToken ct = default)
    {
        _user.RequireStaff();
        return await QueryAsync(query, unassignedOnly: true, ct);
    }

    public async Task<TicketResponse> GetByIdAsync(int id, CancellationToken ct = default)
        => _builder.Build(await _access.GetAccessibleAsync(id, ct));

    public async Task<IReadOnlyList<TicketHistoryResponse>> GetHistoryAsync(int id, CancellationToken ct = default)
    {
        await _access.GetAccessibleAsync(id, ct); // access check
        var history = await _tickets.GetHistoryAsync(id, ct);
        return history.Select(h => h.ToResponse()).ToList();
    }

    private async Task<PagedResult<TicketSummaryResponse>> QueryAsync(
        TicketQuery query, bool unassignedOnly, CancellationToken ct)
    {
        await _queryValidator.ValidateAndThrowAsync(query, ct);
        var me = _user.RequireEmployeeId();

        // Employees: only their own tickets. Agents: tickets of their queues (or assigned to them).
        // Managers / administrators: everything.
        int? requesterId;
        int? visibleToAgent = null;
        if (_user.IsManagerOrAdmin())
        {
            requesterId = query.RequesterEmployeeId;
        }
        else if (_user.IsStaff())
        {
            visibleToAgent = me;
            requesterId = query.RequesterEmployeeId;
        }
        else
        {
            requesterId = me;
        }

        var filter = new TicketFilter(
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            query.Status,
            query.Priority,
            query.CategoryId,
            query.ServiceItemId,
            query.QueueId,
            query.AssignedToMe && _user.IsStaff() ? me : null,
            unassignedOnly,
            requesterId,
            visibleToAgent,
            query.CreatedFrom.HasValue ? DateTime.SpecifyKind(query.CreatedFrom.Value.Date, DateTimeKind.Utc) : null,
            query.CreatedTo.HasValue ? DateTime.SpecifyKind(query.CreatedTo.Value.Date.AddDays(1), DateTimeKind.Utc) : null);

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var (items, total) = await _tickets.GetPagedAsync(filter, page, pageSize, ct);

        return new PagedResult<TicketSummaryResponse>(
            items.Select(t => t.ToSummary()).ToList(), page, pageSize, total);
    }

    // ------------------------------------------------------------------ commands

    public async Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var requesterId = _user.RequireEmployeeId(); // always from the token, never from the body

        var service = await _services.GetByIdAsync(request.ServiceItemId, ct)
                      ?? throw new NotFoundException(nameof(ServiceItem), request.ServiceItemId);
        if (!service.IsActive || !service.Category.IsActive)
            throw new DomainException("This service is not currently available.");

        if (request.AssetId.HasValue && !await _assets.AssetExistsAsync(request.AssetId.Value, ct))
            throw new NotFoundException("Asset", request.AssetId.Value);

        var queue = await ResolveQueueAsync(service, ct);
        var priority = request.Priority ?? service.DefaultPriority;
        var number = await _numbers.NextAsync(ct);

        var ticket = new Ticket(
            number, request.Title, request.Description, requesterId, service, queue, priority, _user.UserId);

        var deadlines = await _sla.CalculateDeadlinesAsync(priority, service.Id, ticket.CreatedAtUtc, ct);
        ticket.SetDeadlines(deadlines.ResponseDueUtc, deadlines.ResolutionDueUtc);

        if (request.AssetId.HasValue)
            ticket.LinkAsset(request.AssetId.Value, _user.UserId);

        await _tickets.AddAsync(ticket, ct);
        await _uow.SaveChangesAsync(ct);

        if (request.AssetId.HasValue)
            await _assets.LinkTicketAsync(request.AssetId.Value, ticket.Id, ct);

        await _notifications.NotifyAsync(
            NotificationType.TicketCreated, requesterId, ticket.Id, $"Ticket {ticket.TicketNumber} was created.", ct);
        await _audit.LogAsync("Create", nameof(Ticket), ticket.Id.ToString(), ticket.TicketNumber, ct);

        return _builder.Build(ticket);
    }

    public async Task<TicketResponse> UpdateAsync(int id, UpdateTicketRequest request, CancellationToken ct = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var ticket = await _access.GetAccessibleAsync(id, ct);
        ticket.EnsureEditable();

        var isStaff = _user.IsStaff();
        if (!isStaff)
        {
            if (ticket.Status is not (TicketStatus.New or TicketStatus.Open or TicketStatus.WaitingForUser))
                throw new DomainException("You can only edit your ticket while it is New, Open or waiting for your reply.");
            if (request.Priority.HasValue && request.Priority.Value != ticket.Priority)
                throw new ForbiddenException("Only service desk staff can change the priority.");
        }

        var userId = _user.UserId;
        ticket.UpdateDetails(request.Title, request.Description, userId);

        var priorityChanged = isStaff
                              && request.Priority.HasValue
                              && ticket.ChangePriority(request.Priority.Value, userId);

        if (priorityChanged)
        {
            var deadlines = await _sla.CalculateDeadlinesAsync(ticket.Priority, ticket.ServiceItemId, ticket.CreatedAtUtc, ct);
            ticket.SetDeadlines(deadlines.ResponseDueUtc, deadlines.ResolutionDueUtc);
        }

        await _uow.SaveChangesAsync(ct);

        if (priorityChanged)
            await _notifications.NotifyAsync(
                NotificationType.PriorityChanged, ticket.RequesterEmployeeId, ticket.Id,
                $"Priority of {ticket.TicketNumber} changed to {ticket.Priority}.", ct);

        await _audit.LogAsync("Update", nameof(Ticket), id.ToString(), ticket.TicketNumber, ct);
        return _builder.Build(ticket);
    }

    public async Task<TicketResponse> ChangeStatusAsync(int id, ChangeStatusRequest request, CancellationToken ct = default)
    {
        await _statusValidator.ValidateAndThrowAsync(request, ct);

        var ticket = await _access.GetAccessibleAsync(id, ct);
        var old = ticket.Status;

        // 1) is the move valid in the workflow at all? (422)
        if (!TicketWorkflow.IsAllowed(old, request.NewStatus))
            throw new DomainException($"A ticket cannot move from {old} to {request.NewStatus}.");

        // 2) is THIS user allowed to make it? (403)
        var allowed = TicketStatusPermissions.AllowedFor(ticket, _user.IsStaff());
        if (!allowed.Contains(request.NewStatus))
            throw new ForbiddenException($"You are not allowed to move this ticket to {request.NewStatus}.");

        ticket.ChangeStatus(request.NewStatus, request.Note, DateTime.UtcNow, _user.UserId);
        await _uow.SaveChangesAsync(ct);

        var type = request.NewStatus switch
        {
            TicketStatus.Resolved => NotificationType.TicketResolved,
            TicketStatus.Closed => NotificationType.TicketClosed,
            _ => NotificationType.StatusChanged
        };
        await _notifications.NotifyAsync(
            type, ticket.RequesterEmployeeId, ticket.Id,
            $"Ticket {ticket.TicketNumber} moved from {old} to {ticket.Status}.", ct);

        await _audit.LogAsync("StatusChange", nameof(Ticket), id.ToString(), $"{old} -> {ticket.Status}", ct);
        return _builder.Build(ticket);
    }

    public async Task<TicketResponse> ReopenAsync(int id, ReopenTicketRequest request, CancellationToken ct = default)
    {
        await _reopenValidator.ValidateAndThrowAsync(request, ct);

        var ticket = await _access.GetAccessibleAsync(id, ct); // requester or staff who can see it
        var window = TimeSpan.FromDays(_policy.ReopenWindowDays);

        ticket.Reopen(request.Reason, DateTime.UtcNow, window, _user.UserId);
        await _uow.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            NotificationType.StatusChanged, ticket.RequesterEmployeeId, ticket.Id,
            $"Ticket {ticket.TicketNumber} was reopened.", ct);
        if (ticket.AssignedAgentEmployeeId.HasValue && ticket.AssignedAgentEmployeeId != ticket.RequesterEmployeeId)
            await _notifications.NotifyAsync(
                NotificationType.StatusChanged, ticket.AssignedAgentEmployeeId.Value, ticket.Id,
                $"Ticket {ticket.TicketNumber} was reopened.", ct);

        await _audit.LogAsync("Reopen", nameof(Ticket), id.ToString(), ticket.TicketNumber, ct);
        return _builder.Build(ticket);
    }

    /// <summary>The service's routing rule decides the queue; the general queue is the fallback.</summary>
    private async Task<TicketQueue> ResolveQueueAsync(ServiceItem service, CancellationToken ct)
    {
        if (service.DefaultQueueId.HasValue)
        {
            var routed = await _queues.GetByIdAsync(service.DefaultQueueId.Value, ct);
            if (routed is { IsActive: true })
                return routed;
        }

        return await _queues.GetByIdAsync(TicketQueue.GeneralQueueId, ct)
               ?? throw new DomainException("No queue is available to receive the ticket.");
    }
}
