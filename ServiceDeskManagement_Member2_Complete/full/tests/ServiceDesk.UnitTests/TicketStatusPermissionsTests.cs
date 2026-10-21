using ServiceDesk.Application.Tickets;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.UnitTests;

public class TicketStatusPermissionsTests
{
    [Fact]
    public void Requester_of_a_new_ticket_may_only_cancel()
    {
        var allowed = TicketStatusPermissions.AllowedFor(Make.Ticket(), isStaff: false);

        Assert.Equal(new[] { TicketStatus.Cancelled }, allowed);
    }

    [Fact]
    public void Staff_may_accept_or_cancel_a_new_ticket()
    {
        var allowed = TicketStatusPermissions.AllowedFor(Make.Ticket(), isStaff: true);

        Assert.Equal(new[] { TicketStatus.Open, TicketStatus.Cancelled }, allowed);
    }

    [Fact]
    public void Requester_may_close_a_resolved_ticket_but_not_reopen_through_the_status_endpoint()
    {
        var ticket = Make.ResolvedTicket(Make.Now);

        Assert.Equal(new[] { TicketStatus.Closed }, TicketStatusPermissions.AllowedFor(ticket, isStaff: false));
        Assert.Equal(new[] { TicketStatus.Closed }, TicketStatusPermissions.AllowedFor(ticket, isStaff: true));
    }

    [Fact]
    public void Requester_may_not_resolve_or_wait()
    {
        var ticket = Make.InProgressTicket();

        Assert.Empty(TicketStatusPermissions.AllowedFor(ticket, isStaff: false));
        Assert.Equal(
            new[] { TicketStatus.WaitingForUser, TicketStatus.Resolved },
            TicketStatusPermissions.AllowedFor(ticket, isStaff: true));
    }

    [Fact]
    public void Requester_can_answer_a_request_for_information()
    {
        var ticket = Make.InProgressTicket();
        ticket.ChangeStatus(TicketStatus.WaitingForUser, "Please send a screenshot", Make.Now, 5);

        Assert.Equal(new[] { TicketStatus.InProgress }, TicketStatusPermissions.AllowedFor(ticket, isStaff: false));
    }

    [Fact]
    public void Nobody_can_move_a_closed_ticket()
    {
        var ticket = Make.ResolvedTicket(Make.Now);
        ticket.ChangeStatus(TicketStatus.Closed, null, Make.Now, 10);

        Assert.Empty(TicketStatusPermissions.AllowedFor(ticket, isStaff: true));
        Assert.Empty(TicketStatusPermissions.AllowedFor(ticket, isStaff: false));
    }
}
