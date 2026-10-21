using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(150);
        builder.Property(a => a.StoredFileName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasIndex(a => a.TicketId);
        builder.HasIndex(a => a.StoredFileName).IsUnique();
    }
}
