namespace Order.Core.Events;

public class OrderCreatedEvent
{
    public Guid OrderId { get; init; }

    public string EventType { get; init; } = "OrderCreated";

    public Guid CorrelationId { get; init; }

    public static OrderCreatedEvent FromOrderId(Guid orderId)
    {
        return new OrderCreatedEvent
        {
            OrderId = orderId,
            CorrelationId = orderId
        };
    }
}
