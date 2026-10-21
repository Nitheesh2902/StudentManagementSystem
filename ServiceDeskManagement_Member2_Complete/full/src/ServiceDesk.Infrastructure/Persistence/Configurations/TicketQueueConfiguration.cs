using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class TicketQueueConfiguration : IEntityTypeConfiguration<TicketQueue>
{
    public void Configure(EntityTypeBuilder<TicketQueue> builder)
    {
        builder.ToTable("TicketQueues");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Name).IsRequired().HasMaxLength(100);
        builder.Property(q => q.Description).HasMaxLength(500);
        builder.Property(q => q.CreatedAtUtc).IsRequired();

        builder.HasIndex(q => q.Name).IsUnique();

        builder.HasMany(q => q.Members)
            .WithOne()
            .HasForeignKey(m => m.QueueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Id 1 is the general queue (TicketQueue.GeneralQueueId): the fallback for tickets without a routing rule.
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var queues = new (int Id, string Name, string Description)[]
        {
            (1, "General Service Desk", "Default queue for tickets without a routing rule"),
            (2, "IT Support", "Hardware, software, network and security requests"),
            (3, "HR", "Human resources requests"),
            (4, "Finance", "Finance requests"),
            (5, "Facilities", "Facilities and administration requests")
        };

        builder.HasData(queues.Select(q => new
        {
            Id = q.Id,
            Name = q.Name,
            Description = (string?)q.Description,
            DepartmentId = (int?)null,
            ManagerEmployeeId = (int?)null,
            IsActive = true,
            CreatedAtUtc = seedDate,
            UpdatedAtUtc = (DateTime?)null
        }));
    }
}
