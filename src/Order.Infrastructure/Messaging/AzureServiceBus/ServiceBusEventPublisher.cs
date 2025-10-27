using Order.Core.Abstractions;
using Order.Core.Events;
using Order.Infrastructure.Messaging.Abstractions;
namespace Order.Infrastructure.Messaging.AzureServiceBus;

public sealed class ServiceBusEventPublisher : IEventPublisher
{
    private readonly IServiceBusPublisher _publisher;

    public ServiceBusEventPublisher(IServiceBusPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task PublishAsync(
        OrderCreatedEvent domainEvent,
        CancellationToken ct = default)
    {
        return _publisher.PublishOrderCreatedAsync(
            domainEvent.OrderId,
            domainEvent.EventType,        // "OrderCreated"
            domainEvent.CorrelationId,    // = OrderId
            ct
        );
    }
}
