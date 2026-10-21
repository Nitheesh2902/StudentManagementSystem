using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class TicketHistoryConfiguration : IEntityTypeConfiguration<TicketHistory>
{
    public void Configure(EntityTypeBuilder<TicketHistory> builder)
    {
        builder.ToTable("TicketHistory");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Action).IsRequired();
        builder.Property(h => h.FieldName).HasMaxLength(100);
        builder.Property(h => h.OldValue).HasMaxLength(500);
        builder.Property(h => h.NewValue).HasMaxLength(500);
        builder.Property(h => h.Note).HasMaxLength(500);
        builder.Property(h => h.ChangedAtUtc).IsRequired();

        builder.HasIndex(h => new { h.TicketId, h.ChangedAtUtc });
    }
}
