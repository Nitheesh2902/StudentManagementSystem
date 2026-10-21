using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.Entities;

public class TicketCategory : BaseEntity
{
    private TicketCategory() { } // EF Core

    public TicketCategory(string name, string? description)
    {
        Apply(name, description);
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public ICollection<ServiceItem> Services { get; private set; } = new List<ServiceItem>();

    public void Update(string name, string? description) => Apply(name, description);

    private void Apply(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
