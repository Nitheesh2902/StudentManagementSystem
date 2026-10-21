using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.UnitTests;

public class TicketCreationTests
{
    [Fact]
    public void New_ticket_starts_as_New_with_a_created_history_entry()
    {
        var ticket = Make.Ticket();

        Assert.Equal(TicketStatus.New, ticket.Status);
        Assert.Equal(10, ticket.RequesterEmployeeId);
        Assert.Single(ticket.History);
        Assert.Equal(TicketHistoryAction.Created, ticket.History.Single().Action);
    }

    [Theory]
    [InlineData("", "text")]
    [InlineData("title", "")]
    [InlineData("   ", "text")]
    public void Title_and_description_are_required(string title, string description)
    {
        Assert.Throws<DomainException>(() => new Ticket(
            "TKT-2026-000001", title, description, 10, Make.Service(), Make.Queue(), TicketPriority.Low, null));
    }

    [Fact]
    public void Update_details_records_one_history_row_per_changed_field()
    {
        var ticket = Make.Ticket();

        ticket.UpdateDetails("New title", ticket.Description, 1);

        var rows = ticket.History.Where(h => h.Action == TicketHistoryAction.Updated).ToList();
        Assert.Single(rows);
        Assert.Equal("Title", rows[0].FieldName);
        Assert.Equal("New title", rows[0].NewValue);
    }

    [Fact]
    public void Changing_priority_to_the_same_value_is_a_no_op()
    {
        var ticket = Make.Ticket(priority: TicketPriority.High);

        Assert.False(ticket.ChangePriority(TicketPriority.High, 1));
        Assert.DoesNotContain(ticket.History, h => h.Action == TicketHistoryAction.PriorityChanged);
    }

    [Fact]
    public void Changing_priority_records_history()
    {
        var ticket = Make.Ticket(priority: TicketPriority.Low);

        Assert.True(ticket.ChangePriority(TicketPriority.Critical, 1));
        Assert.Equal(TicketPriority.Critical, ticket.Priority);
        Assert.Contains(ticket.History, h => h.Action == TicketHistoryAction.PriorityChanged && h.NewValue == "Critical");
    }
}

public class TicketStatusTests
{
    [Fact]
    public void Resolving_requires_a_note()
    {
        var ticket = Make.InProgressTicket();

        Assert.Throws<DomainException>(() => ticket.ChangeStatus(TicketStatus.Resolved, "  ", Make.Now, 5));
    }

    [Fact]
    public void Resolving_stores_the_note_and_the_time()
    {
        var ticket = Make.InProgressTicket();

        ticket.ChangeStatus(TicketStatus.Resolved, "Reset the profile", Make.Now, 5);

        Assert.Equal(TicketStatus.Resolved, ticket.Status);
        Assert.Equal(Make.Now, ticket.ResolvedAtUtc);
        Assert.Equal("Reset the profile", ticket.ResolutionNote);
    }

    [Fact]
    public void Closing_stores_the_closed_time()
    {
        var ticket = Make.ResolvedTicket(Make.Now);

        ticket.ChangeStatus(TicketStatus.Closed, null, Make.Now.AddDays(1), 10);

        Assert.Equal(TicketStatus.Closed, ticket.Status);
        Assert.Equal(Make.Now.AddDays(1), ticket.ClosedAtUtc);
    }

    [Fact]
    public void A_move_the_workflow_does_not_allow_is_rejected()
    {
        var ticket = Make.Ticket(); // New

        var ex = Assert.Throws<DomainException>(() => ticket.ChangeStatus(TicketStatus.Resolved, "x", Make.Now, 1));
        Assert.Contains("cannot move", ex.Message);
    }

    [Theory]
    [InlineData(TicketStatus.Assigned)]
    [InlineData(TicketStatus.Reopened)]
    public void Assigned_and_Reopened_have_their_own_actions(TicketStatus status)
    {
        var ticket = Make.Ticket();

        Assert.Throws<DomainException>(() => ticket.ChangeStatus(status, "x", Make.Now, 1));
    }

    [Fact]
    public void Every_status_change_is_written_to_the_history()
    {
        var ticket = Make.InProgressTicket();

        Assert.Contains(ticket.History, h => h.Action == TicketHistoryAction.StatusChanged && h.NewValue == "InProgress");
    }

    [Fact]
    public void A_final_ticket_can_no_longer_be_edited()
    {
        var ticket = Make.ResolvedTicket(Make.Now);

        Assert.Throws<DomainException>(() => ticket.UpdateDetails("a", "b", 1));
        Assert.Throws<DomainException>(() => ticket.ChangePriority(TicketPriority.Low, 1));
    }
}

public class TicketReopenTests
{
    private static readonly TimeSpan Window = TimeSpan.FromDays(7);

    [Fact]
    public void Reopening_inside_the_window_works_and_is_counted()
    {
        var ticket = Make.ResolvedTicket(Make.Now);

        ticket.Reopen("Still broken", Make.Now.AddDays(3), Window, 10);

        Assert.Equal(TicketStatus.Reopened, ticket.Status);
        Assert.Equal(1, ticket.ReopenCount);
        Assert.Null(ticket.ResolvedAtUtc);
        Assert.Null(ticket.ResolutionNote);
        Assert.Contains(ticket.History, h => h.Action == TicketHistoryAction.Reopened && h.Note == "Still broken");
    }

    [Fact]
    public void Reopening_after_the_window_is_rejected()
    {
        var ticket = Make.ResolvedTicket(Make.Now);

        var ex = Assert.Throws<DomainException>(() => ticket.Reopen("Late", Make.Now.AddDays(8), Window, 10));
        Assert.Contains("reopen window", ex.Message);
    }

    [Fact]
    public void Only_a_resolved_ticket_can_be_reopened()
    {
        var ticket = Make.InProgressTicket();

        Assert.Throws<DomainException>(() => ticket.Reopen("why", Make.Now, Window, 10));
    }

    [Fact]
    public void Reopening_needs_a_reason()
    {
        var ticket = Make.ResolvedTicket(Make.Now);

        Assert.Throws<DomainException>(() => ticket.Reopen(" ", Make.Now.AddDays(1), Window, 10));
    }

    [Fact]
    public void A_reopened_ticket_goes_back_to_work_via_InProgress()
    {
        var ticket = Make.ResolvedTicket(Make.Now);
        ticket.Reopen("Still broken", Make.Now.AddDays(1), Window, 10);

        ticket.ChangeStatus(TicketStatus.InProgress, null, Make.Now.AddDays(1), 5);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
    }

    [Fact]
    public void CanReopen_is_true_only_inside_the_window()
    {
        var ticket = Make.ResolvedTicket(Make.Now);

        Assert.True(ticket.CanReopen(Make.Now.AddDays(6), Window));
        Assert.False(ticket.CanReopen(Make.Now.AddDays(8), Window));
        Assert.False(Make.Ticket().CanReopen(Make.Now, Window));
    }
}

public class TicketAssignmentTests
{
    [Fact]
    public void Assigning_a_new_ticket_accepts_it_and_marks_it_assigned()
    {
        var ticket = Make.Ticket();

        ticket.AssignTo(5, "Take this one", 5, Make.Now);

        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.Equal(5, ticket.AssignedAgentEmployeeId);
        Assert.Contains(ticket.History, h => h.Action == TicketHistoryAction.Assigned);
        Assert.Single(ticket.Assignments);
        Assert.Equal(AssignmentType.Assigned, ticket.Assignments.Single().Type);
    }

    [Fact]
    public void Assigning_to_the_same_agent_again_is_rejected()
    {
        var ticket = Make.Ticket();
        ticket.AssignTo(5, null, 5, Make.Now);

        Assert.Throws<DomainException>(() => ticket.AssignTo(5, null, 5, Make.Now));
    }

    [Fact]
    public void Reassigning_keeps_the_status_and_is_recorded_as_a_reassignment()
    {
        var ticket = Make.InProgressTicket();

        ticket.AssignTo(6, "Going on leave", 1, Make.Now);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(6, ticket.AssignedAgentEmployeeId);
        Assert.Contains(ticket.History, h => h.Action == TicketHistoryAction.Reassigned && h.OldValue == "5" && h.NewValue == "6");
        Assert.Equal(AssignmentType.Reassigned, ticket.Assignments.Last().Type);
    }

    [Fact]
    public void A_resolved_ticket_cannot_be_assigned()
    {
        var ticket = Make.ResolvedTicket(Make.Now);

        Assert.Throws<DomainException>(() => ticket.AssignTo(6, null, 1, Make.Now));
    }

    [Fact]
    public void Escalating_to_another_queue_clears_the_assignee_and_returns_the_ticket_to_Open()
    {
        var ticket = Make.InProgressTicket();
        var target = Make.Queue(2, "IT Support");

        ticket.Escalate(target, null, "Needs network team", 1, Make.Now);

        Assert.Equal(2, ticket.QueueId);
        Assert.Null(ticket.AssignedAgentEmployeeId);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.True(ticket.IsEscalated);
        Assert.Contains(ticket.History, h => h.Action == TicketHistoryAction.Escalated);
        Assert.Equal(AssignmentType.Escalated, ticket.Assignments.Last().Type);
    }

    [Fact]
    public void Escalating_to_a_manager_assigns_the_manager()
    {
        var ticket = Make.InProgressTicket();

        ticket.Escalate(null, 99, "Customer is a VIP", 5, Make.Now);

        Assert.Equal(99, ticket.AssignedAgentEmployeeId);
        Assert.True(ticket.IsEscalated);
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
    }

    [Fact]
    public void Escalation_needs_exactly_one_target_and_a_reason()
    {
        var ticket = Make.InProgressTicket();

        Assert.Throws<DomainException>(() => ticket.Escalate(null, null, "reason", 1, Make.Now));
        Assert.Throws<DomainException>(() => ticket.Escalate(Make.Queue(2), 99, "reason", 1, Make.Now));
        Assert.Throws<DomainException>(() => ticket.Escalate(Make.Queue(2), null, " ", 1, Make.Now));
    }

    [Fact]
    public void Escalating_to_the_queue_it_is_already_in_is_rejected()
    {
        var ticket = Make.InProgressTicket();

        Assert.Throws<DomainException>(() => ticket.Escalate(Make.Queue(1), null, "reason", 1, Make.Now));
    }
}

public class TicketCommentAndAttachmentTests
{
    [Fact]
    public void Adding_a_comment_is_recorded_in_the_history()
    {
        var ticket = Make.Ticket();

        var comment = ticket.AddComment(10, "Any update?", false, 1);

        Assert.False(comment.IsInternal);
        Assert.Single(ticket.Comments);
        Assert.Contains(ticket.History, h => h.Action == TicketHistoryAction.CommentAdded && h.NewValue == "Comment");
    }

    [Fact]
    public void Internal_notes_are_marked_in_the_history()
    {
        var ticket = Make.Ticket();

        ticket.AddComment(5, "Check the logs", true, 5);

        Assert.Contains(ticket.History, h => h.NewValue == "Internal note");
    }

    [Fact]
    public void An_empty_comment_is_rejected()
    {
        var ticket = Make.Ticket();

        Assert.Throws<DomainException>(() => ticket.AddComment(10, "  ", false, 1));
    }

    [Fact]
    public void A_closed_ticket_accepts_no_comments_or_attachments()
    {
        var ticket = Make.ResolvedTicket(Make.Now);
        ticket.ChangeStatus(TicketStatus.Closed, null, Make.Now, 10);

        Assert.Throws<DomainException>(() => ticket.AddComment(10, "late", false, 1));
        Assert.Throws<DomainException>(() => ticket.AddAttachment("a.txt", "x.txt", "text/plain", 3, 10, 1));
    }

    [Fact]
    public void A_resolved_ticket_still_accepts_comments_so_the_requester_can_reply()
    {
        var ticket = Make.ResolvedTicket(Make.Now);

        ticket.AddComment(10, "That did not fix it", false, 1);

        Assert.Single(ticket.Comments);
    }

    [Fact]
    public void Adding_and_removing_an_attachment_is_recorded()
    {
        var ticket = Make.Ticket();

        var attachment = ticket.AddAttachment("screenshot.png", "abc.png", "image/png", 1234, 10, 1);
        Assert.True(attachment.IsActive);

        ticket.RemoveAttachment(attachment, 1);

        Assert.False(attachment.IsActive);
        Assert.Contains(ticket.History, h => h.Action == TicketHistoryAction.AttachmentAdded);
        Assert.Contains(ticket.History, h => h.Action == TicketHistoryAction.AttachmentRemoved);
    }

    [Fact]
    public void An_empty_attachment_is_rejected()
    {
        var ticket = Make.Ticket();

        Assert.Throws<DomainException>(() => ticket.AddAttachment("a.txt", "x.txt", "text/plain", 0, 10, 1));
    }
}

public class TicketQueueTests
{
    [Fact]
    public void The_general_queue_cannot_be_deactivated()
    {
        var general = Make.Queue(TicketQueue.GeneralQueueId);

        Assert.Throws<DomainException>(() => general.Deactivate());
    }

    [Fact]
    public void Other_queues_can_be_deactivated()
    {
        var queue = Make.Queue(2, "IT Support");

        queue.Deactivate();

        Assert.False(queue.IsActive);
    }

    [Fact]
    public void Members_can_be_added_removed_and_re_added()
    {
        var queue = Make.Queue(2, "IT Support");

        queue.AddMember(5);
        queue.AddMember(6);
        Assert.Equal(new[] { 5, 6 }, queue.ActiveMemberIds);

        queue.RemoveMember(5);
        Assert.Equal(new[] { 6 }, queue.ActiveMemberIds);

        queue.AddMember(5); // reactivates the old membership
        Assert.Equal(new[] { 5, 6 }, queue.ActiveMemberIds);
        Assert.Equal(2, queue.Members.Count);
    }

    [Fact]
    public void Adding_the_same_active_member_twice_conflicts()
    {
        var queue = Make.Queue(2, "IT Support");
        queue.AddMember(5);

        Assert.Throws<ConflictException>(() => queue.AddMember(5));
    }

    [Fact]
    public void Removing_someone_who_is_not_a_member_is_not_found()
    {
        var queue = Make.Queue(2, "IT Support");

        Assert.Throws<NotFoundException>(() => queue.RemoveMember(77));
    }
}
