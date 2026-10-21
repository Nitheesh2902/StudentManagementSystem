
using EmployeeServiceDesk.Application.Abstractions.Persistence;

using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Entities;

namespace EmployeeServiceDesk.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public const string TicketNumberSequence = "TicketNumberSeq";

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Member 2: Ticket Operations
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<TicketQueue> TicketQueues => Set<TicketQueue>();
    public DbSet<TicketQueueMember> TicketQueueMembers => Set<TicketQueueMember>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketAssignment> TicketAssignments => Set<TicketAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        modelBuilder.HasSequence<int>(TicketNumberSequence)
            .StartsAt(1)
            .IncrementsBy(1);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
                     .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAtUtc = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
