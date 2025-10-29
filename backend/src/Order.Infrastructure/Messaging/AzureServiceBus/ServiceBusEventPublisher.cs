using Order.Core.Abstractions;
using Order.Core.Events;
using Order.Infrastructure.Messaging;

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
            domainEvent.EventType,
            domainEvent.CorrelationId,
            ct
        );
    }

    public async Task PublishSameEventNTimesAsync(OrderCreatedEvent domainEvent, int n = 5, CancellationToken ct = default)
    {
        for (int i = 0; i < n; i++)
        {
            await _publisher.PublishOrderCreatedAsync(
                domainEvent.OrderId,
                domainEvent.EventType,
                domainEvent.CorrelationId,
                ct
            );
        }
    }
}
