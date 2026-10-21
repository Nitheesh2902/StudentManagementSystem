using ServiceDesk.Application.Catalog.Dtos;
using ServiceDesk.Application.Catalog.Validators;
using ServiceDesk.Application.Queues.Dtos;
using ServiceDesk.Application.Queues.Validators;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Application.Tickets.Validators;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.UnitTests;

public class TicketValidatorTests
{
    [Fact]
    public void A_valid_create_request_passes()
    {
        var request = new CreateTicketRequest("VPN down", "Cannot connect since this morning", 1, TicketPriority.High, null);

        Assert.True(new CreateTicketRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("", "text", 1)]
    [InlineData("title", "", 1)]
    [InlineData("title", "text", 0)]
    public void Create_request_rejects_missing_values(string title, string description, int serviceId)
    {
        var request = new CreateTicketRequest(title, description, serviceId, null, null);

        Assert.False(new CreateTicketRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Create_request_rejects_a_too_long_title_and_an_unknown_priority()
    {
        Assert.False(new CreateTicketRequestValidator()
            .Validate(new CreateTicketRequest(new string('x', 201), "text", 1, null, null)).IsValid);
        Assert.False(new CreateTicketRequestValidator()
            .Validate(new CreateTicketRequest("title", "text", 1, (TicketPriority)99, null)).IsValid);
    }

    [Fact]
    public void Resolving_without_a_note_is_invalid_but_other_statuses_do_not_need_one()
    {
        var validator = new ChangeStatusRequestValidator();

        Assert.False(validator.Validate(new ChangeStatusRequest(TicketStatus.Resolved, null)).IsValid);
        Assert.False(validator.Validate(new ChangeStatusRequest(TicketStatus.Resolved, "  ")).IsValid);
        Assert.True(validator.Validate(new ChangeStatusRequest(TicketStatus.Resolved, "Fixed")).IsValid);
        Assert.True(validator.Validate(new ChangeStatusRequest(TicketStatus.InProgress, null)).IsValid);
    }

    [Fact]
    public void Reopen_needs_a_reason()
    {
        var validator = new ReopenTicketRequestValidator();

        Assert.False(validator.Validate(new ReopenTicketRequest("")).IsValid);
        Assert.True(validator.Validate(new ReopenTicketRequest("Still broken")).IsValid);
    }

    [Fact]
    public void Escalation_needs_exactly_one_target()
    {
        var validator = new EscalateTicketRequestValidator();

        Assert.True(validator.Validate(new EscalateTicketRequest(2, null, "reason")).IsValid);
        Assert.True(validator.Validate(new EscalateTicketRequest(null, 9, "reason")).IsValid);
        Assert.False(validator.Validate(new EscalateTicketRequest(2, 9, "reason")).IsValid);
        Assert.False(validator.Validate(new EscalateTicketRequest(null, null, "reason")).IsValid);
        Assert.False(validator.Validate(new EscalateTicketRequest(2, null, "")).IsValid);
    }

    [Fact]
    public void Query_dates_must_be_in_order()
    {
        var validator = new TicketQueryValidator();

        Assert.True(validator.Validate(new TicketQuery
        {
            CreatedFrom = new DateTime(2026, 1, 1),
            CreatedTo = new DateTime(2026, 1, 31)
        }).IsValid);

        Assert.False(validator.Validate(new TicketQuery
        {
            CreatedFrom = new DateTime(2026, 2, 1),
            CreatedTo = new DateTime(2026, 1, 1)
        }).IsValid);
    }

    [Fact]
    public void Comment_text_is_required_and_limited()
    {
        var validator = new CreateCommentRequestValidator();

        Assert.False(validator.Validate(new CreateCommentRequest("", false)).IsValid);
        Assert.False(validator.Validate(new CreateCommentRequest(new string('x', 2001), false)).IsValid);
        Assert.True(validator.Validate(new CreateCommentRequest("Looks good", true)).IsValid);
    }

    [Fact]
    public void Assign_and_reassign_need_an_agent()
    {
        Assert.False(new AssignTicketRequestValidator().Validate(new AssignTicketRequest(0, null)).IsValid);
        Assert.True(new AssignTicketRequestValidator().Validate(new AssignTicketRequest(5, null)).IsValid);
        Assert.False(new ReassignTicketRequestValidator().Validate(new ReassignTicketRequest(5, "")).IsValid);
        Assert.True(new ReassignTicketRequestValidator().Validate(new ReassignTicketRequest(5, "On leave")).IsValid);
    }
}

public class CatalogAndQueueValidatorTests
{
    [Fact]
    public void Category_name_is_required_and_limited()
    {
        var validator = new CreateCategoryRequestValidator();

        Assert.False(validator.Validate(new CreateCategoryRequest("", null)).IsValid);
        Assert.False(validator.Validate(new CreateCategoryRequest(new string('x', 101), null)).IsValid);
        Assert.True(validator.Validate(new CreateCategoryRequest("Security", "Security requests")).IsValid);
    }

    [Fact]
    public void Service_needs_a_category_and_a_valid_priority()
    {
        var validator = new CreateServiceItemRequestValidator();

        Assert.True(validator.Validate(new CreateServiceItemRequest("VPN", null, 1, TicketPriority.Medium, null)).IsValid);
        Assert.False(validator.Validate(new CreateServiceItemRequest("VPN", null, 0, TicketPriority.Medium, null)).IsValid);
        Assert.False(validator.Validate(new CreateServiceItemRequest("VPN", null, 1, (TicketPriority)42, null)).IsValid);
        Assert.False(validator.Validate(new CreateServiceItemRequest("VPN", null, 1, TicketPriority.Low, 0)).IsValid);
    }

    [Fact]
    public void Queue_name_is_required_and_members_need_a_positive_id()
    {
        Assert.False(new CreateQueueRequestValidator().Validate(new CreateQueueRequest("", null, null, null)).IsValid);
        Assert.True(new CreateQueueRequestValidator().Validate(new CreateQueueRequest("Network", null, null, null)).IsValid);
        Assert.False(new AddQueueMemberRequestValidator().Validate(new AddQueueMemberRequest(0)).IsValid);
        Assert.True(new AddQueueMemberRequestValidator().Validate(new AddQueueMemberRequest(7)).IsValid);
    }
}
