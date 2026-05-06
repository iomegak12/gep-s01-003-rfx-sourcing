using EventService.Domain.Entities;
using EventService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventService.Repositories;

/// <summary>
/// EF Core DbContext for the Event Service. Aggregate roots and join entities
/// are added to this context as feature slices are implemented (see
/// <c>docs/impl-event-service-plan.md</c>).
/// </summary>
public class EventDbContext : DbContext
{
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options)
    {
    }

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<LineItem> LineItems => Set<LineItem>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<EventSupplier> EventSuppliers => Set<EventSupplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Supplier>(b =>
        {
            b.ToTable("suppliers");
            b.HasKey(s => s.Id);

            b.Property(s => s.Id).HasColumnName("id").IsRequired();
            b.Property(s => s.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            b.Property(s => s.ContactEmail).HasColumnName("contact_email").IsRequired().HasMaxLength(200);
            b.Property(s => s.IsActive).HasColumnName("is_active").IsRequired();
            b.Property(s => s.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

            b.HasIndex(s => s.Name)
             .IsUnique()
             .HasDatabaseName("ux_suppliers_name");
        });

        modelBuilder.Entity<Event>(b =>
        {
            b.ToTable("events");
            b.HasKey(e => e.Id);

            b.Property(e => e.Id).HasColumnName("id").IsRequired();
            b.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(200);
            b.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            b.Property(e => e.Category).HasColumnName("category").IsRequired().HasMaxLength(80);
            b.Property(e => e.Currency).HasColumnName("currency").IsRequired().HasMaxLength(3);
            b.Property(e => e.ResponseDeadlineUtc).HasColumnName("response_deadline_utc").IsRequired();
            b.Property(e => e.Status).HasColumnName("status").IsRequired()
                .HasConversion<int>();
            b.Property(e => e.Version).HasColumnName("version").IsRequired();
            b.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired().HasMaxLength(200);
            b.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            b.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        });

        modelBuilder.Entity<LineItem>(b =>
        {
            b.ToTable("line_items");
            b.HasKey(l => l.Id);

            b.Property(l => l.Id).HasColumnName("id").IsRequired();
            b.Property(l => l.EventId).HasColumnName("event_id").IsRequired();
            b.Property(l => l.Description).HasColumnName("description").IsRequired().HasMaxLength(500);
            b.Property(l => l.Quantity).HasColumnName("quantity").IsRequired();
            b.Property(l => l.UnitPrice).HasColumnName("unit_price").IsRequired();
            b.Property(l => l.Currency).HasColumnName("currency").IsRequired().HasMaxLength(3);
            b.Property(l => l.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

            b.HasOne(l => l.Event)
             .WithMany()
             .HasForeignKey(l => l.EventId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(l => l.EventId).HasDatabaseName("ix_line_items_event_id");
        });

        modelBuilder.Entity<AuditEvent>(b =>
        {
            b.ToTable("audit_events");
            b.HasKey(a => a.Id);

            b.Property(a => a.Id).HasColumnName("id").IsRequired();
            b.Property(a => a.EventId).HasColumnName("event_id").IsRequired();
            b.Property(a => a.Action).HasColumnName("action").IsRequired()
                .HasConversion<int>();
            b.Property(a => a.CorrelationId).HasColumnName("correlation_id").IsRequired().HasMaxLength(200);
            b.Property(a => a.PerformedByUserId).HasColumnName("performed_by_user_id").IsRequired().HasMaxLength(200);
            b.Property(a => a.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
            b.Property(a => a.Payload).HasColumnName("payload");

            b.HasIndex(a => a.EventId).HasDatabaseName("ix_audit_events_event_id");
        });

        modelBuilder.Entity<EventSupplier>(b =>
        {
            b.ToTable("event_suppliers");
            b.HasKey(es => new { es.EventId, es.SupplierId });

            b.Property(es => es.EventId).HasColumnName("event_id").IsRequired();
            b.Property(es => es.SupplierId).HasColumnName("supplier_id").IsRequired();
            b.Property(es => es.InvitedByUserId).HasColumnName("invited_by_user_id").IsRequired().HasMaxLength(200);
            b.Property(es => es.InvitedAtUtc).HasColumnName("invited_at_utc").IsRequired();

            b.HasOne(es => es.Supplier)
             .WithMany()
             .HasForeignKey(es => es.SupplierId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(es => es.EventId).HasDatabaseName("ix_event_suppliers_event_id");
        });
    }
}
