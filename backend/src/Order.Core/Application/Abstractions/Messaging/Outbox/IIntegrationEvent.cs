namespace Order.Core.Application.Abstractions.Messaging.Outbox;


public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredOnUtc { get; }
    string Type { get; }
    string? CorrelationId { get; }
}
