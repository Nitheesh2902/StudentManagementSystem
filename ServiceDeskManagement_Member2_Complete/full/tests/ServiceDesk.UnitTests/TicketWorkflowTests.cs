using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Workflow;

namespace ServiceDesk.UnitTests;

public class TicketWorkflowTests
{
    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Open, true)]
    [InlineData(TicketStatus.New, TicketStatus.Cancelled, true)]
    [InlineData(TicketStatus.New, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.Open, TicketStatus.Assigned, true)]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Open, TicketStatus.Cancelled, true)]
    [InlineData(TicketStatus.Open, TicketStatus.Closed, false)]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Assigned, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.InProgress, TicketStatus.WaitingForUser, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Closed, false)]
    [InlineData(TicketStatus.WaitingForUser, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.WaitingForUser, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Reopened, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress, false)]
    [InlineData(TicketStatus.Reopened, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Reopened, TicketStatus.Resolved, false)]
    public void IsAllowed_follows_the_lifecycle(TicketStatus from, TicketStatus to, bool expected)
        => Assert.Equal(expected, TicketWorkflow.IsAllowed(from, to));

    [Theory]
    [InlineData(TicketStatus.Closed)]
    [InlineData(TicketStatus.Cancelled)]
    public void Closed_and_cancelled_are_terminal(TicketStatus status)
        => Assert.Empty(TicketWorkflow.NextFrom(status));

    [Fact]
    public void Every_status_is_defined_in_the_workflow()
    {
        foreach (var status in Enum.GetValues<TicketStatus>())
            Assert.NotNull(TicketWorkflow.NextFrom(status));
    }
}
