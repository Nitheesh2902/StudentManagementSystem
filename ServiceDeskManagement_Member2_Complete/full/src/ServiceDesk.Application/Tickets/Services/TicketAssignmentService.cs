using EmployeeServiceDesk.Application.Abstractions.Persistence;
using FluentValidation;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Application.Common;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Tickets.Services;

public class TicketAssignmentService : ITicketAssignmentService
{
    private readonly IQueueRepository _queues;
    private readonly ITicketAccessService _access;
    private readonly ITicketResponseBuilder _builder;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _user;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;
    private readonly IValidator<AssignTicketRequest> _assignValidator;
    private readonly IValidator<ReassignTicketRequest> _reassignValidator;
    private readonly IValidator<EscalateTicketRequest> _escalateValidator;

    public TicketAssignmentService(
        IQueueRepository queues,
        ITicketAccessService access,
        ITicketResponseBuilder builder,
        IUnitOfWork uow,
        ICurrentUserService user,
        INotificationService notifications,
        IAuditService audit,
        IValidator<AssignTicketRequest> assignValidator,
        IValidator<ReassignTicketRequest> reassignValidator,
        IValidator<EscalateTicketRequest> escalateValidator)
    {
        _queues = queues;
        _access = access;
        _builder = builder;
        _uow = uow;
        _user = user;
        _notifications = notifications;
        _audit = audit;
        _assignValidator = assignValidator;
        _reassignValidator = reassignValidator;
        _escalateValidator = escalateValidator;
    }

    public async Task<TicketResponse> AssignAsync(int ticketId, AssignTicketRequest request, CancellationToken ct = default)
    {
        await _assignValidator.ValidateAndThrowAsync(request, ct);
        _user.RequireStaff();
        var me = _user.RequireEmployeeId();

        var ticket = await _access.GetAccessibleAsync(ticketId, ct);

        if (ticket.AssignedAgentEmployeeId.HasValue)
            throw new ConflictException("This ticket is already assigned. Use reassign to hand it to someone else.");

        // Agents pick up work for themselves; managers and administrators can assign to anyone.
        if (!_user.IsManagerOrAdmin() && request.AgentEmployeeId != me)
            throw new ForbiddenException(
                "Agents can only assign tickets to themselves. Ask a manager to assign it to someone else.");

        await EnsureQueueMemberAsync(ticket, request.AgentEmployeeId, ct);

        ticket.AssignTo(request.AgentEmployeeId, request.Note, _user.UserId, DateTime.UtcNow);
        await _uow.SaveChangesAsync(ct);

        await NotifyAssignedAsync(ticket, ct);
        await _audit.LogAsync("Assign", nameof(Ticket), ticket.Id.ToString(), $"Agent={request.AgentEmployeeId}", ct);
        return _builder.Build(ticket);
    }

    public async Task<TicketResponse> ReassignAsync(int ticketId, ReassignTicketRequest request, CancellationToken ct = default)
    {
        await _reassignValidator.ValidateAndThrowAsync(request, ct);
        _user.RequireStaff();
        var me = _user.RequireEmployeeId();

        var ticket = await _access.GetAccessibleAsync(ticketId, ct);

        if (!ticket.AssignedAgentEmployeeId.HasValue)
            throw new DomainException("This ticket is not assigned yet. Use assign instead.");

        if (!_user.IsManagerOrAdmin() && ticket.AssignedAgentEmployeeId != me)
            throw new ForbiddenException("Only a manager or the current assignee can reassign this ticket.");

        await EnsureQueueMemberAsync(ticket, request.AgentEmployeeId, ct);

        ticket.AssignTo(request.AgentEmployeeId, request.Reason, _user.UserId, DateTime.UtcNow);
        await _uow.SaveChangesAsync(ct);

        await NotifyAssignedAsync(ticket, ct);
        await _audit.LogAsync("Reassign", nameof(Ticket), ticket.Id.ToString(), $"Agent={request.AgentEmployeeId}", ct);
        return _builder.Build(ticket);
    }

    public async Task<TicketResponse> EscalateAsync(int ticketId, EscalateTicketRequest request, CancellationToken ct = default)
    {
        await _escalateValidator.ValidateAndThrowAsync(request, ct);
        _user.RequireStaff();

        var ticket = await _access.GetAccessibleAsync(ticketId, ct);

        TicketQueue? targetQueue = null;
        if (request.TargetQueueId.HasValue)
        {
            targetQueue = await _queues.GetByIdAsync(request.TargetQueueId.Value, ct)
                          ?? throw new NotFoundException(nameof(TicketQueue), request.TargetQueueId.Value);
            if (!targetQueue.IsActive)
                throw new DomainException("A ticket cannot be escalated to an inactive queue.");
        }

        ticket.Escalate(targetQueue, request.ManagerEmployeeId, request.Reason, _user.UserId, DateTime.UtcNow);
        await _uow.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            NotificationType.TicketEscalated, ticket.RequesterEmployeeId, ticket.Id,
            $"Ticket {ticket.TicketNumber} was escalated.", ct);
        if (request.ManagerEmployeeId.HasValue && request.ManagerEmployeeId.Value != ticket.RequesterEmployeeId)
            await _notifications.NotifyAsync(
                NotificationType.TicketEscalated, request.ManagerEmployeeId.Value, ticket.Id,
                $"Ticket {ticket.TicketNumber} was escalated to you.", ct);

        await _audit.LogAsync("Escalate", nameof(Ticket), ticket.Id.ToString(), request.Reason, ct);
        return _builder.Build(ticket);
    }

    private async Task EnsureQueueMemberAsync(Ticket ticket, int agentEmployeeId, CancellationToken ct)
    {
        if (!await _queues.IsActiveMemberAsync(ticket.QueueId, agentEmployeeId, ct))
            throw new DomainException(
                $"Employee {agentEmployeeId} is not a member of queue '{ticket.Queue.Name}'. Add them to the queue first.");
    }

    private async Task NotifyAssignedAsync(Ticket ticket, CancellationToken ct)
    {
        var agent = ticket.AssignedAgentEmployeeId!.Value;

        await _notifications.NotifyAsync(
            NotificationType.TicketAssigned, ticket.RequesterEmployeeId, ticket.Id,
            $"Ticket {ticket.TicketNumber} was assigned to an agent.", ct);

        if (agent != ticket.RequesterEmployeeId)
            await _notifications.NotifyAsync(
                NotificationType.TicketAssigned, agent, ticket.Id,
                $"Ticket {ticket.TicketNumber} was assigned to you.", ct);
    }
}
