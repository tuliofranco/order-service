using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;

using Order.Infrastructure.Messaging;
using Xunit;

namespace Order.Infrastructure.Tests;

public class ServiceBusEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_DeveEncaminharEventoCorretamenteParaPublisherInterno()
    {
        // arrange
        var publisherMock = new Mock<IServiceBusPublisher>();

        var svc = new ServiceBusEventPublisher(publisherMock.Object);

        var evt = OrderCreatedEvent.FromOrderId(Guid.NewGuid());
        var ct = CancellationToken.None;

        // act
        await svc.PublishAsync(evt, ct);

        // assert
        publisherMock.Verify(p =>
            p.PublishOrderCreatedAsync(
                evt.OrderId,
                evt.EventType,
                evt.CorrelationId,
                ct
            ),
            Times.Once,
            "PublishAsync deve repassar o evento 1x com os mesmos dados"
        );
    }

    [Fact]
    public async Task PublishSameEventNTimesAsync_DevePublicarMesmoEventoNVezes()
    {
        // arrange
        var publisherMock = new Mock<IServiceBusPublisher>();
        var svc = new ServiceBusEventPublisher(publisherMock.Object);

        var evt = OrderCreatedEvent.FromOrderId(Guid.NewGuid());
        var ct = CancellationToken.None;
        var n = 5;

        // act
        await svc.PublishSameEventNTimesAsync(evt, n, ct);

        // assert
        publisherMock.Verify(p =>
            p.PublishOrderCreatedAsync(
                evt.OrderId,
                evt.EventType,
                evt.CorrelationId,
                ct
            ),
            Times.Exactly(n),
            "PublishSameEventNTimesAsync deve publicar o mesmo evento repetidamente"
        );
    }
}
