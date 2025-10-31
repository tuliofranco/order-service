namespace Order.Core.Application.Abstractions.Messaging.Outbox;

public interface IEventMapper
{
    // Diz se o mapper sabe transformar este DomainEvent específico.
    bool CanMap(object domainEvent);

    // Transforma um DomainEvent em um IIntegrationEvent pronto para ir ao outbox.
    IIntegrationEvent Map(object domainEvent);
}
