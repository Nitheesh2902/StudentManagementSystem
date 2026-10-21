using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class TicketCategoryConfiguration : IEntityTypeConfiguration<TicketCategory>
{
    public void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.ToTable("TicketCategories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => c.Name).IsUnique();

        // Categories required by the specification.
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var names = new[]
        {
            "IT Support", "Hardware", "Software", "HR",
            "Finance", "Facilities", "Network", "Security"
        };

        builder.HasData(names.Select((name, i) => new
        {
            Id = i + 1,
            Name = name,
            Description = (string?)null,
            IsActive = true,
            CreatedAtUtc = seedDate,
            UpdatedAtUtc = (DateTime?)null
        }));
    }
}
