using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;
using ServiceDesk.Domain.Workflow;

namespace ServiceDesk.Domain.Entities;

public class Ticket : BaseEntity
{
    private Ticket() { } // EF Core

    public Ticket(
        string ticketNumber, string title, string description, int requesterEmployeeId,
        ServiceItem service, TicketQueue queue, TicketPriority priority, int? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber)) throw new DomainException("Ticket number is required.");
        if (requesterEmployeeId <= 0) throw new DomainException("Requester is required.");
        if (!Enum.IsDefined(priority)) throw new DomainException("Invalid priority.");

        TicketNumber = ticketNumber;
        RequesterEmployeeId = requesterEmployeeId;
        ServiceItem = service;
        ServiceItemId = service.Id;
        Category = service.Category;
        CategoryId = service.CategoryId;
        Queue = queue;
        QueueId = queue.Id;
        Priority = priority;
        Status = TicketStatus.New;
        SetText(title, description);

        AddHistory(TicketHistoryAction.Created, null, null, ticketNumber, createdByUserId);
    }

    public string TicketNumber { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    // Cross-module reference: Employee is owned by Member 1, so no navigation/FK yet.
    public int RequesterEmployeeId { get; private set; }

    public int CategoryId { get; private set; }
    public TicketCategory Category { get; private set; } = default!;
    public int ServiceItemId { get; private set; }
    public ServiceItem ServiceItem { get; private set; } = default!;

    public int QueueId { get; private set; }
    public TicketQueue Queue { get; private set; } = default!;
    public int? AssignedAgentEmployeeId { get; private set; }
    public bool IsEscalated { get; private set; }

    public TicketPriority Priority { get; private set; }
    public TicketStatus Status { get; private set; }

    public DateTime? DueResponseAtUtc { get; private set; }
    public DateTime? DueResolutionAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public string? ResolutionNote { get; private set; }
    public int ReopenCount { get; private set; }

    public int? AssetId { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public ICollection<TicketHistory> History { get; private set; } = new List<TicketHistory>();
    public ICollection<TicketComment> Comments { get; private set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; private set; } = new List<TicketAttachment>();
    public ICollection<TicketAssignment> Assignments { get; private set; } = new List<TicketAssignment>();

    public bool IsFinal => Status is TicketStatus.Resolved or TicketStatus.Closed or TicketStatus.Cancelled;

    public void EnsureEditable()
    {
        if (IsFinal)
            throw new DomainException($"A ticket that is {Status} can no longer be edited.");
    }

    // ---------------------------------------------------------------- status workflow

    /// <summary>
    /// Moves the ticket to a new status if the workflow allows it.
    /// Reopened and Assigned have their own actions (Reopen / AssignTo).
    /// </summary>
    public void ChangeStatus(TicketStatus next, string? note, DateTime nowUtc, int? changedByUserId)
    {
        if (next == TicketStatus.Reopened)
            throw new DomainException("Use the reopen action to reopen a ticket.");
        if (next == TicketStatus.Assigned)
            throw new DomainException("A ticket becomes Assigned when it is assigned to an agent.");

        Transition(next, note, nowUtc, changedByUserId, TicketHistoryAction.StatusChanged);
    }

    public bool CanReopen(DateTime nowUtc, TimeSpan window)
        => Status == TicketStatus.Resolved
           && ResolvedAtUtc.HasValue
           && nowUtc - ResolvedAtUtc.Value <= window;

    public void Reopen(string reason, DateTime nowUtc, TimeSpan window, int? changedByUserId)
    {
        if (Status != TicketStatus.Resolved)
            throw new DomainException("Only a resolved ticket can be reopened.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A reason is required to reopen a ticket.");
        if (!CanReopen(nowUtc, window))
            throw new DomainException(
                $"This ticket can no longer be reopened: the reopen window of {window.TotalDays:0} day(s) has passed.");

        Transition(TicketStatus.Reopened, reason, nowUtc, changedByUserId, TicketHistoryAction.Reopened);
        ResolvedAtUtc = null;
        ResolutionNote = null;
        ReopenCount++;
    }

    private void Transition(
        TicketStatus next, string? note, DateTime nowUtc, int? changedByUserId, TicketHistoryAction action)
    {
        if (!TicketWorkflow.IsAllowed(Status, next))
            throw new DomainException($"A ticket cannot move from {Status} to {next}.");
        if (next == TicketStatus.Resolved && string.IsNullOrWhiteSpace(note))
            throw new DomainException("A resolution note is required when resolving a ticket.");

        var old = Status;
        Status = next;

        switch (next)
        {
            case TicketStatus.Resolved:
                ResolvedAtUtc = nowUtc;
                ResolutionNote = note!.Trim();
                break;
            case TicketStatus.Closed:
                ClosedAtUtc = nowUtc;
                break;
        }

        AddHistory(action, nameof(Status), old.ToString(), next.ToString(), changedByUserId, note?.Trim());
    }

    /// <summary>System-driven status reset (e.g. an escalation returns the ticket to a queue). Not user-callable.</summary>
    private void ForceStatus(TicketStatus next, string? note, int? changedByUserId)
    {
        if (Status == next) return;
        var old = Status;
        Status = next;
        AddHistory(TicketHistoryAction.StatusChanged, nameof(Status), old.ToString(), next.ToString(), changedByUserId, note);
    }

    // ---------------------------------------------------------------- assignment

    /// <summary>
    /// Assigns the ticket to an agent (first assignment) or hands it to another agent (reassignment).
    /// A New ticket is accepted (Open) and then Assigned; an Open ticket becomes Assigned.
    /// </summary>
    public void AssignTo(int agentEmployeeId, string? note, int? changedByUserId, DateTime nowUtc)
    {
        if (agentEmployeeId <= 0) throw new DomainException("Agent is required.");
        EnsureNotFinal("assigned");

        var previous = AssignedAgentEmployeeId;
        if (previous == agentEmployeeId)
            throw new DomainException("The ticket is already assigned to this agent.");

        var type = previous is null ? AssignmentType.Assigned : AssignmentType.Reassigned;

        if (Status == TicketStatus.New)
            Transition(TicketStatus.Open, "Accepted by the service desk", nowUtc, changedByUserId, TicketHistoryAction.StatusChanged);

        AssignedAgentEmployeeId = agentEmployeeId;

        if (Status == TicketStatus.Open)
            Transition(TicketStatus.Assigned, note, nowUtc, changedByUserId, TicketHistoryAction.StatusChanged);

        Assignments.Add(new TicketAssignment(type, previous, agentEmployeeId, QueueId, QueueId, note, changedByUserId));
        AddHistory(
            type == AssignmentType.Assigned ? TicketHistoryAction.Assigned : TicketHistoryAction.Reassigned,
            "AssignedAgent", previous?.ToString(), agentEmployeeId.ToString(), changedByUserId, note);
    }

    /// <summary>
    /// Escalates the ticket either to another team queue (assignee cleared, ticket returns to Open)
    /// or to a manager (the manager becomes the assignee). Exactly one target must be given.
    /// </summary>
    public void Escalate(
        TicketQueue? targetQueue, int? managerEmployeeId, string reason, int? changedByUserId, DateTime nowUtc)
    {
        EnsureNotFinal("escalated");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A reason is required to escalate a ticket.");
        if ((targetQueue is null) == (managerEmployeeId is null))
            throw new DomainException("Escalate either to another queue or to a manager, not both.");

        var previousAgent = AssignedAgentEmployeeId;

        if (targetQueue is not null)
        {
            if (targetQueue.Id == QueueId) throw new DomainException("The ticket is already in that queue.");

            var fromQueue = QueueId;
            Queue = targetQueue;
            QueueId = targetQueue.Id;
            AssignedAgentEmployeeId = null;
            IsEscalated = true;

            if (Status != TicketStatus.New)
                ForceStatus(TicketStatus.Open, "Returned to the new queue after escalation", changedByUserId);

            Assignments.Add(new TicketAssignment(
                AssignmentType.Escalated, previousAgent, null, fromQueue, targetQueue.Id, reason, changedByUserId));
            AddHistory(
                TicketHistoryAction.Escalated, "Queue", fromQueue.ToString(), targetQueue.Id.ToString(),
                changedByUserId, reason);
            return;
        }

        var manager = managerEmployeeId!.Value;
        if (manager <= 0) throw new DomainException("Manager is required.");
        if (previousAgent == manager) throw new DomainException("The ticket is already assigned to this manager.");

        IsEscalated = true;
        if (Status == TicketStatus.New)
            Transition(TicketStatus.Open, "Accepted by the service desk", nowUtc, changedByUserId, TicketHistoryAction.StatusChanged);

        AssignedAgentEmployeeId = manager;
        if (Status == TicketStatus.Open)
            Transition(TicketStatus.Assigned, reason, nowUtc, changedByUserId, TicketHistoryAction.StatusChanged);

        Assignments.Add(new TicketAssignment(
            AssignmentType.Escalated, previousAgent, manager, QueueId, QueueId, reason, changedByUserId));
        AddHistory(
            TicketHistoryAction.Escalated, "AssignedAgent", previousAgent?.ToString(), manager.ToString(),
            changedByUserId, reason);
    }

    // ---------------------------------------------------------------- comments and attachments

    public TicketComment AddComment(int authorEmployeeId, string body, bool isInternal, int? changedByUserId)
    {
        EnsureNotClosed();

        var comment = new TicketComment(authorEmployeeId, body, isInternal);
        Comments.Add(comment);
        AddHistory(TicketHistoryAction.CommentAdded, null, null, isInternal ? "Internal note" : "Comment", changedByUserId);
        return comment;
    }

    public TicketAttachment AddAttachment(
        string fileName, string storedFileName, string contentType, long sizeBytes,
        int uploadedByEmployeeId, int? changedByUserId)
    {
        EnsureNotClosed();

        var attachment = new TicketAttachment(fileName, storedFileName, contentType, sizeBytes, uploadedByEmployeeId);
        Attachments.Add(attachment);
        AddHistory(TicketHistoryAction.AttachmentAdded, "Attachment", null, attachment.FileName, changedByUserId);
        return attachment;
    }

    public void RemoveAttachment(TicketAttachment attachment, int? changedByUserId)
    {
        EnsureNotClosed();

        attachment.Deactivate();
        AddHistory(TicketHistoryAction.AttachmentRemoved, "Attachment", attachment.FileName, null, changedByUserId);
    }

    // ---------------------------------------------------------------- details

    /// <summary>Updates title and description, recording one history row per changed field.</summary>
    public void UpdateDetails(string title, string description, int? changedByUserId)
    {
        EnsureEditable();

        var oldTitle = Title;
        var oldDescription = Description;
        SetText(title, description);

        if (oldTitle != Title)
            AddHistory(TicketHistoryAction.Updated, nameof(Title), oldTitle, Title, changedByUserId);
        if (oldDescription != Description)
            AddHistory(TicketHistoryAction.Updated, nameof(Description), oldDescription, Description, changedByUserId);
    }

    /// <returns>true when the priority actually changed.</returns>
    public bool ChangePriority(TicketPriority newPriority, int? changedByUserId)
    {
        EnsureEditable();
        if (!Enum.IsDefined(newPriority)) throw new DomainException("Invalid priority.");
        if (newPriority == Priority) return false;

        var old = Priority;
        Priority = newPriority;
        AddHistory(TicketHistoryAction.PriorityChanged, nameof(Priority), old.ToString(), newPriority.ToString(), changedByUserId);
        return true;
    }

    public void SetDeadlines(DateTime? responseDueUtc, DateTime? resolutionDueUtc)
    {
        DueResponseAtUtc = responseDueUtc;
        DueResolutionAtUtc = resolutionDueUtc;
    }

    public void LinkAsset(int assetId, int? changedByUserId)
    {
        if (assetId <= 0) throw new DomainException("Invalid asset.");
        AssetId = assetId;
        AddHistory(TicketHistoryAction.Updated, nameof(AssetId), null, assetId.ToString(), changedByUserId);
    }

    public void AddHistory(
        TicketHistoryAction action, string? fieldName, string? oldValue, string? newValue,
        int? changedByUserId, string? note = null)
        => History.Add(new TicketHistory(action, fieldName, oldValue, newValue, changedByUserId, note));

    // ---------------------------------------------------------------- helpers

    private void EnsureNotFinal(string verb)
    {
        if (IsFinal)
            throw new DomainException($"A ticket that is {Status} cannot be {verb}.");
    }

    private void EnsureNotClosed()
    {
        if (Status is TicketStatus.Closed or TicketStatus.Cancelled)
            throw new DomainException($"This ticket is {Status}; it can no longer be changed.");
    }

    private void SetText(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Title is required.");
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("Description is required.");
        Title = title.Trim();
        Description = description.Trim();
    }
}
