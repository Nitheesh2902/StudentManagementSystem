using FluentValidation;
using ServiceDesk.Application.Queues.Dtos;

namespace ServiceDesk.Application.Queues.Validators;

public class CreateQueueRequestValidator : AbstractValidator<CreateQueueRequest>
{
    public CreateQueueRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DepartmentId).GreaterThan(0).When(x => x.DepartmentId.HasValue);
        RuleFor(x => x.ManagerEmployeeId).GreaterThan(0).When(x => x.ManagerEmployeeId.HasValue);
    }
}

public class UpdateQueueRequestValidator : AbstractValidator<UpdateQueueRequest>
{
    public UpdateQueueRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DepartmentId).GreaterThan(0).When(x => x.DepartmentId.HasValue);
        RuleFor(x => x.ManagerEmployeeId).GreaterThan(0).When(x => x.ManagerEmployeeId.HasValue);
    }
}

public class AddQueueMemberRequestValidator : AbstractValidator<AddQueueMemberRequest>
{
    public AddQueueMemberRequestValidator()
    {
        RuleFor(x => x.AgentEmployeeId).GreaterThan(0);
    }
}
