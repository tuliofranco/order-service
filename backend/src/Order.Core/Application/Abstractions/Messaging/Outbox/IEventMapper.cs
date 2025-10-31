namespace Order.Core.Application.Abstractions.Messaging.Outbox;

public interface IEventMapper
{
    bool CanMap(object domainEvent);
    IIntegrationEvent Map(object domainEvent);
}
