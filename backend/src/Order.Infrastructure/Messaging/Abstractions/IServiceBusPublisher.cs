namespace Order.Infrastructure.Messaging;
public interface IServiceBusPublisher
{
    Task PublishOrderCreatedAsync(
        Guid orderId,
        string eventType,
        Guid correlationId,
        CancellationToken ct = default);
}