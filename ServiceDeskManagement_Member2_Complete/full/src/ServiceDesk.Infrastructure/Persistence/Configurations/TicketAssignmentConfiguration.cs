using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class TicketAssignmentConfiguration : IEntityTypeConfiguration<TicketAssignment>
{
    public void Configure(EntityTypeBuilder<TicketAssignment> builder)
    {
        builder.ToTable("TicketAssignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type).IsRequired();
        builder.Property(a => a.Note).HasMaxLength(500);
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasIndex(a => new { a.TicketId, a.CreatedAtUtc });
        builder.HasIndex(a => a.ToAgentEmployeeId);
    }
}
