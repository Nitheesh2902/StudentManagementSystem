using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class TicketQueueMemberConfiguration : IEntityTypeConfiguration<TicketQueueMember>
{
    public void Configure(EntityTypeBuilder<TicketQueueMember> builder)
    {
        builder.ToTable("TicketQueueMembers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.CreatedAtUtc).IsRequired();

        builder.HasIndex(m => new { m.QueueId, m.AgentEmployeeId }).IsUnique();
        builder.HasIndex(m => m.AgentEmployeeId);
    }
}
