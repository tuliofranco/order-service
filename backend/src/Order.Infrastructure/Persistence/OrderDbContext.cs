using Microsoft.EntityFrameworkCore;
using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Domain.Entities;
using Order.Infrastructure.Persistence.Entities;

namespace Order.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
public OrderDbContext(DbContextOptions<OrderDbContext> options)
: base(options)
{
}

public DbSet<OrderEntity> Orders => Set<OrderEntity>();
public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<OrderEntity>(cfg =>
    {
        cfg.ToTable("orders");

        cfg.HasKey(o => o.Id);

        cfg.Property(o => o.Id)
           .HasColumnName("id");

        cfg.Property(o => o.ClienteNome)
           .HasColumnName("cliente_nome")
           .IsRequired()
           .HasMaxLength(200);

        cfg.Property(o => o.Produto)
           .HasColumnName("produto")
           .IsRequired()
           .HasMaxLength(200);

        cfg.Property(o => o.Valor)
           .HasColumnName("valor")
           .HasColumnType("numeric(18,2)")
           .IsRequired();

        cfg.Property(o => o.Status)
           .HasColumnName("status")
           .HasConversion<string>()
           .IsRequired();

       cfg.Property(o => o.data_criacao)
          .HasColumnName("data_criacao")
          .IsRequired();

         cfg.Property(o => o.data_de_efetivacao)
           .HasColumnName("data_de_efetivacao")
           .IsRequired(false);
    });

    modelBuilder.Entity<ProcessedMessage>(b =>
    {
        b.ToTable("processed_messages");

        b.HasKey(x => x.MessageId);

        b.Property(x => x.MessageId)
         .HasColumnName("message_id")
         .IsRequired();

        b.Property(x => x.ProcessedAtUtc)
         .HasColumnName("processed_at_utc")
         .IsRequired();
    });

    modelBuilder.Entity<OutboxMessage>(cfg =>
    {
        cfg.ToTable("outbox_messages");

        cfg.HasKey(x => x.Id);

        cfg.Property(x => x.Id)
           .HasColumnName("id")
           .IsRequired();

        cfg.Property(x => x.Type)
           .HasColumnName("type")
           .HasMaxLength(256)
           .IsRequired();

        cfg.Property(x => x.Payload)
           .HasColumnName("payload")
           .HasColumnType("text")
           .IsRequired();

        cfg.Property(x => x.OccurredOnUtc)
           .HasColumnName("occurred_on_utc")
           .IsRequired();

        cfg.HasIndex(x => x.OccurredOnUtc)
           .HasDatabaseName("ix_outbox_occurred_on_utc");
    });

    modelBuilder.Entity<OrderStatusHistory>(cfg =>
    {
        cfg.ToTable("order_status_history");

        cfg.HasKey(x => x.Id);

        cfg.Property(x => x.Id)
           .HasColumnName("id")
           .IsRequired();

        cfg.Property(x => x.OrderId)
           .HasColumnName("order_id")
           .IsRequired();

        cfg.Property(x => x.FromStatus)
           .HasColumnName("from_status")
           .HasConversion<string?>();

        cfg.Property(x => x.ToStatus)
           .HasColumnName("to_status")
           .HasConversion<string>()
           .IsRequired();

        cfg.Property(x => x.OccurredAt)
           .HasColumnName("occurred_at_utc")
           .IsRequired();

        cfg.Property(x => x.Source)
           .HasColumnName("source")
           .HasMaxLength(32)
           .IsRequired();

        cfg.Property(x => x.CorrelationId)
           .HasColumnName("correlation_id")
           .HasMaxLength(64)
           .IsRequired();

        cfg.Property(x => x.EventId)
           .HasColumnName("event_id")
           .HasMaxLength(64)
           .IsRequired();

        cfg.HasOne<OrderEntity>()
           .WithMany()
           .HasForeignKey(x => x.OrderId)
           .OnDelete(DeleteBehavior.NoAction);

        cfg.HasIndex(x => x.OrderId)
           .HasDatabaseName("ix_order_status_history_order_id");

        cfg.HasIndex(x => x.OccurredAt)
           .HasDatabaseName("ix_order_status_history_occurred_at");

        cfg.HasIndex(x => new { x.CorrelationId, x.EventId })
           .HasDatabaseName("ix_order_status_history_correlation_event");
    });
}

}