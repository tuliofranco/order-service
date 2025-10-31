namespace Order.Core.Application.Abstractions.Messaging.Outbox;


public interface IIntegrationEvent
{
    Guid Id { get; }                 // Identificador único do evento
    DateTime OccurredOnUtc { get; }  // Momento em que o fato ocorreu (UTC)
    string Type { get; }             // Nome lógico/versionado do evento (ex.: "OrderCreated.v1")
    string? CorrelationId { get; }   // Correlação ponta-a-ponta (HTTP request, saga, etc.)
}
