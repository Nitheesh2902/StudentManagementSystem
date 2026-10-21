using FluentValidation;
using ServiceDesk.Application.Catalog.Dtos;

namespace ServiceDesk.Application.Catalog.Validators;

public class CreateServiceItemRequestValidator : AbstractValidator<CreateServiceItemRequest>
{
    public CreateServiceItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.DefaultPriority).IsInEnum();
        RuleFor(x => x.DefaultQueueId).GreaterThan(0).When(x => x.DefaultQueueId.HasValue);
    }
}

public class UpdateServiceItemRequestValidator : AbstractValidator<UpdateServiceItemRequest>
{
    public UpdateServiceItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.DefaultPriority).IsInEnum();
        RuleFor(x => x.DefaultQueueId).GreaterThan(0).When(x => x.DefaultQueueId.HasValue);
    }
}
