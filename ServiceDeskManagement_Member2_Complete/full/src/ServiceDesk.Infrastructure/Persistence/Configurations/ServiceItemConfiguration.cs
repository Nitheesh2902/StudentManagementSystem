using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceDesk.Domain.Entities;

namespace ServiceDesk.Infrastructure.Persistence.Configurations;

public class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.DefaultPriority).IsRequired();
        builder.Property(s => s.CreatedAtUtc).IsRequired();

        builder.HasOne(s => s.Category)
            .WithMany(c => c.Services)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TicketQueue>()
            .WithMany()
            .HasForeignKey(s => s.DefaultQueueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.CategoryId, s.Name }).IsUnique();
        builder.HasIndex(s => s.IsActive);
    }
}
