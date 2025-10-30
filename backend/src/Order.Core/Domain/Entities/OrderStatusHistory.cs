using Order.Core.Domain.Entities.Enums;

using System;


namespace Order.Core.Domain.Entities;

public sealed class OrderStatusHistory
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid OrderId { get; init; }
    public OrderStatus? FromStatus { get; init; }
    public OrderStatus ToStatus { get; init; }

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = "Api";

    public string? CorrelationId { get; init; }
    public string? EventId { get; init; }

    public static OrderStatusHistory Create(
        Guid orderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        string? correlationId,
        string? eventId,
        string source,
        string? reason = null,
        DateTimeOffset? occurredAt = null)
        => new()
        {
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            CorrelationId = correlationId,
            EventId = eventId,
            Source = source,
            OccurredAt = occurredAt ?? DateTimeOffset.UtcNow
        };
}
