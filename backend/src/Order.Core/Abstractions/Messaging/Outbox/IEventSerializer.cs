namespace Order.Core.Abstractions.Messaging.Outbox;

public interface IEventSerializer
{
    string Serialize(IIntegrationEvent @event);
    IIntegrationEvent Deserialize(string payload, string type);
}
