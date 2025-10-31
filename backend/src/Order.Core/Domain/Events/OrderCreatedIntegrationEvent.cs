using System;
using Order.Core.Application.Abstractions.Messaging.Outbox;


namespace Order.Core.Domain.Events;

public sealed record OrderCreatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string Type,
    string? CorrelationId,
    string? CausationId,
    // Payload do evento
    Guid OrderId,
    string Cliente,
    string Produto,
    decimal Valor
) : IIntegrationEvent
{

    public static OrderCreatedIntegrationEvent Create(
        Guid orderId,
        string cliente,
        string produto,
        decimal valor,
        string? correlationId = null,
        string? causationId = null,
        string type = "OrderCreated.v1")
    {
        return new OrderCreatedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            Type: type,
            CorrelationId: correlationId ?? orderId.ToString(),
            CausationId: causationId,
            OrderId: orderId,
            Cliente: cliente,
            Produto: produto,
            Valor: valor
        );
    }
}
