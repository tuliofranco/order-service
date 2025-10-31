using System;
using FluentAssertions;


namespace Order.Core.Tests;

public class OrderCreatedEventTests
{
    [Fact]
    public void FromOrderId_DevePopularOsCamposCorretamente()
    {
        // arrange
        var orderId = Guid.NewGuid();

        // act
        var evt = OrderCreatedEvent.FromOrderId(orderId);

        // assert
        evt.Should().NotBeNull();

        evt.OrderId.Should().Be(orderId, "o evento precisa carregar o ID real do pedido");
        evt.CorrelationId.Should().Be(orderId, "a correlação padrão é o próprio OrderId");
        evt.EventType.Should().Be("OrderCreated", "esse é o nome do evento que o worker/Service Bus vai consumir");
    }

    [Fact]
    public void EventType_DeveSerSempreOrderCreatedPorPadrao()
    {
        // arrange
        var orderId = Guid.NewGuid();

        // act
        var evt = OrderCreatedEvent.FromOrderId(orderId);

        // assert
        evt.EventType.Should().Be("OrderCreated");
    }

    [Fact]
    public void CorrelationId_DeveSerIgualAoOrderId()
    {
        // arrange
        var orderId = Guid.NewGuid();

        // act
        var evt = OrderCreatedEvent.FromOrderId(orderId);

        // assert
        evt.CorrelationId.Should().Be(evt.OrderId);
    }
}
