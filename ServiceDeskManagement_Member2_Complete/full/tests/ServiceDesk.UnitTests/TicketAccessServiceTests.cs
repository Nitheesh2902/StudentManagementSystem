using ServiceDesk.Application.Tickets;
using ServiceDesk.Application.Tickets.Services;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.UnitTests;

public class TicketAccessServiceTests
{
    private const string Agent = "ServiceDeskAgent";
    private const string Manager = "ServiceDeskManager";
    private const string Admin = "Administrator";

    private static TicketAccessService Create(
        Domain.Entities.Ticket ticket, FakeUser user, params (int QueueId, int EmployeeId)[] members)
        => new(new FakeTicketRepository(ticket), new FakeQueueRepository(members), user);

    [Fact]
    public async Task The_requester_sees_their_own_ticket()
    {
        var ticket = Make.Ticket(requester: 10);
        var service = Create(ticket, new FakeUser { EmployeeId = 10, Role = "Employee" });

        Assert.Same(ticket, await service.GetAccessibleAsync(1));
    }

    [Fact]
    public async Task Another_employee_gets_not_found()
    {
        var ticket = Make.Ticket(requester: 10);
        var service = Create(ticket, new FakeUser { EmployeeId = 11, Role = "Employee" });

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAccessibleAsync(1));
    }

    [Fact]
    public async Task An_unknown_ticket_is_not_found()
    {
        var service = Create(Make.Ticket(), new FakeUser());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAccessibleAsync(999));
    }

    [Fact]
    public async Task An_agent_sees_tickets_of_a_queue_they_belong_to()
    {
        var ticket = Make.Ticket(requester: 10, queue: Make.Queue(3, "IT Support"));
        var service = Create(ticket, new FakeUser { EmployeeId = 5, Role = Agent }, (3, 5));

        Assert.Same(ticket, await service.GetAccessibleAsync(1));
    }

    [Fact]
    public async Task An_agent_does_not_see_tickets_of_other_queues()
    {
        var ticket = Make.Ticket(requester: 10, queue: Make.Queue(3, "IT Support"));
        var service = Create(ticket, new FakeUser { EmployeeId = 5, Role = Agent }, (4, 5));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAccessibleAsync(1));
    }

    [Fact]
    public async Task An_agent_sees_a_ticket_assigned_to_them_even_without_membership()
    {
        var ticket = Make.Ticket(requester: 10);
        ticket.AssignTo(5, null, 5, Make.Now);
        var service = Create(ticket, new FakeUser { EmployeeId = 5, Role = Agent });

        Assert.Same(ticket, await service.GetAccessibleAsync(1));
    }

    [Theory]
    [InlineData(Manager)]
    [InlineData(Admin)]
    public async Task Managers_and_administrators_see_everything(string role)
    {
        var ticket = Make.Ticket(requester: 10);
        var service = Create(ticket, new FakeUser { EmployeeId = 77, Role = role });

        Assert.Same(ticket, await service.GetAccessibleAsync(1));
    }

    [Fact]
    public async Task A_signed_out_user_is_rejected()
    {
        var service = Create(Make.Ticket(), new FakeUser { UserId = null, EmployeeId = null, Role = null });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAccessibleAsync(1));
    }
}

public class TicketResponseBuilderTests
{
    [Fact]
    public void The_response_lists_what_the_current_user_may_do_next()
    {
        var ticket = Make.InProgressTicket();

        var asEmployee = new TicketResponseBuilder(new FakeUser { Role = "Employee" }, new FakePolicy()).Build(ticket);
        var asAgent = new TicketResponseBuilder(new FakeUser { Role = "ServiceDeskAgent" }, new FakePolicy()).Build(ticket);

        Assert.Empty(asEmployee.AllowedNextStatuses);
        Assert.Equal(new[] { TicketStatus.WaitingForUser, TicketStatus.Resolved }, asAgent.AllowedNextStatuses);
    }

    [Fact]
    public void CanReopen_follows_the_configured_window()
    {
        var ticket = Make.ResolvedTicket(DateTime.UtcNow.AddDays(-3));

        var inside = new TicketResponseBuilder(new FakeUser(), new FakePolicy { ReopenWindowDays = 7 }).Build(ticket);
        var outside = new TicketResponseBuilder(new FakeUser(), new FakePolicy { ReopenWindowDays = 2 }).Build(ticket);

        Assert.True(inside.CanReopen);
        Assert.False(outside.CanReopen);
    }

    [Fact]
    public void The_response_carries_queue_and_assignee()
    {
        var ticket = Make.Ticket();
        ticket.AssignTo(5, null, 5, Make.Now);

        var response = new TicketResponseBuilder(new FakeUser(), new FakePolicy()).Build(ticket);

        Assert.Equal(5, response.AssignedAgentEmployeeId);
        Assert.Equal("General Service Desk", response.QueueName);
        Assert.Equal(TicketStatus.Assigned, response.Status);
    }
}
