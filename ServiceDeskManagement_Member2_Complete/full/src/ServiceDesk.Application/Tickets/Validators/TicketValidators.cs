using FluentValidation;
using ServiceDesk.Application.Tickets.Dtos;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.Tickets.Validators;

public class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.ServiceItemId).GreaterThan(0);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.AssetId).GreaterThan(0).When(x => x.AssetId.HasValue);
    }
}

public class UpdateTicketRequestValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class ChangeStatusRequestValidator : AbstractValidator<ChangeStatusRequest>
{
    public ChangeStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(2000);
        RuleFor(x => x.Note).NotEmpty()
            .When(x => x.NewStatus == TicketStatus.Resolved)
            .WithMessage("A resolution note is required when resolving a ticket.");
    }
}

public class ReopenTicketRequestValidator : AbstractValidator<ReopenTicketRequest>
{
    public ReopenTicketRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class TicketQueryValidator : AbstractValidator<TicketQuery>
{
    public TicketQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x).Must(q => !q.CreatedFrom.HasValue || !q.CreatedTo.HasValue || q.CreatedFrom <= q.CreatedTo)
            .WithMessage("CreatedFrom must be on or before CreatedTo.");
    }
}

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public class AssignTicketRequestValidator : AbstractValidator<AssignTicketRequest>
{
    public AssignTicketRequestValidator()
    {
        RuleFor(x => x.AgentEmployeeId).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class ReassignTicketRequestValidator : AbstractValidator<ReassignTicketRequest>
{
    public ReassignTicketRequestValidator()
    {
        RuleFor(x => x.AgentEmployeeId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class EscalateTicketRequestValidator : AbstractValidator<EscalateTicketRequest>
{
    public EscalateTicketRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TargetQueueId).GreaterThan(0).When(x => x.TargetQueueId.HasValue);
        RuleFor(x => x.ManagerEmployeeId).GreaterThan(0).When(x => x.ManagerEmployeeId.HasValue);
        RuleFor(x => x)
            .Must(x => x.TargetQueueId.HasValue ^ x.ManagerEmployeeId.HasValue)
            .WithMessage("Provide either targetQueueId or managerEmployeeId, not both and not neither.");
    }
}
