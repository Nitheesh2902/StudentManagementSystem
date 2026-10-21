using EmployeeServiceDesk.Application.Abstractions.Persistence;
using FluentValidation;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Application.Common;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Tickets.Services;

public class TicketCommentService : ITicketCommentService
{
    private readonly ITicketCommentRepository _comments;
    private readonly ITicketAccessService _access;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _user;
    private readonly INotificationService _notifications;
    private readonly IValidator<CreateCommentRequest> _validator;

    public TicketCommentService(
        ITicketCommentRepository comments,
        ITicketAccessService access,
        IUnitOfWork uow,
        ICurrentUserService user,
        INotificationService notifications,
        IValidator<CreateCommentRequest> validator)
    {
        _comments = comments;
        _access = access;
        _uow = uow;
        _user = user;
        _notifications = notifications;
        _validator = validator;
    }

    public async Task<IReadOnlyList<CommentResponse>> GetAsync(int ticketId, CancellationToken ct = default)
    {
        await _access.GetAccessibleAsync(ticketId, ct);

        var comments = await _comments.GetByTicketAsync(ticketId, includeInternal: _user.IsStaff(), ct);
        return comments.Select(c => c.ToResponse()).ToList();
    }

    public async Task<CommentResponse> AddAsync(int ticketId, CreateCommentRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        var ticket = await _access.GetAccessibleAsync(ticketId, ct);
        var me = _user.RequireEmployeeId();

        if (request.IsInternal && !_user.IsStaff())
            throw new ForbiddenException("Only service desk staff can write internal notes.");

        var comment = ticket.AddComment(me, request.Body, request.IsInternal, _user.UserId);

        // The requester answering a request for information puts the ticket back to work.
        if (!request.IsInternal
            && me == ticket.RequesterEmployeeId
            && ticket.Status == TicketStatus.WaitingForUser)
        {
            ticket.ChangeStatus(TicketStatus.InProgress, "Requester replied", DateTime.UtcNow, _user.UserId);
        }

        await _uow.SaveChangesAsync(ct);

        if (!request.IsInternal)
        {
            // Tell the other side of the conversation.
            int? recipient = me == ticket.RequesterEmployeeId
                ? ticket.AssignedAgentEmployeeId
                : ticket.RequesterEmployeeId;

            if (recipient.HasValue && recipient.Value != me)
                await _notifications.NotifyAsync(
                    NotificationType.CommentAdded, recipient.Value, ticket.Id,
                    $"New comment on ticket {ticket.TicketNumber}.", ct);
        }

        return comment.ToResponse();
    }
}
