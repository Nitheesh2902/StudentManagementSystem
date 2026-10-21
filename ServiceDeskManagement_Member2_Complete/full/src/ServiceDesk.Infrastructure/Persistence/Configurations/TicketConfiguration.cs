using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketNumber).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(4000);
        builder.Property(t => t.ResolutionNote).HasMaxLength(2000);
        builder.Property(t => t.Priority).IsRequired();
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ServiceItem)
            .WithMany()
            .HasForeignKey(t => t.ServiceItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Queue)
            .WithMany()
            .HasForeignKey(t => t.QueueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.History)
            .WithOne()
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Comments)
            .WithOne()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Attachments)
            .WithOne()
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Assignments)
            .WithOne()
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TicketNumber).IsUnique();
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.RequesterEmployeeId);
        builder.HasIndex(t => t.AssignedAgentEmployeeId);
        builder.HasIndex(t => t.QueueId);
        builder.HasIndex(t => t.CategoryId);
        builder.HasIndex(t => t.ServiceItemId);
        builder.HasIndex(t => t.CreatedAtUtc);
    }
}
