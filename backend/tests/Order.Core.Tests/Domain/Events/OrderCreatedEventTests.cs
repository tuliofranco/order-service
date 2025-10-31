using System;
using System.Threading.Tasks;
using FluentAssertions;
using Order.Core.Domain.Events;
using Xunit;

namespace Order.Core.Tests.Domain.Events;

public class OrderCreatedEventTests
{
    [Fact]
    public void Create_DevePopularCamposPadrao_CorrelationIdPadraoETypeV1()
    {
        var orderId = Guid.NewGuid();
        var cliente = "Tulio";
        var produto = "Boleto";
        var valor   = 300m;

        var before = DateTime.UtcNow;
        var evt = OrderCreatedIntegrationEvent.Create(orderId, cliente, produto, valor);
        var after  = DateTime.UtcNow;

        evt.Id.Should().NotBeEmpty();
        evt.OccurredOnUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        evt.OccurredOnUtc.Kind.Should().Be(DateTimeKind.Utc);

        evt.Type.Should().Be("OrderCreated.v1");
        evt.CorrelationId.Should().Be(orderId.ToString());
        evt.CausationId.Should().BeNull();

        evt.OrderId.Should().Be(orderId);
        evt.Cliente.Should().Be(cliente);
        evt.Produto.Should().Be(produto);
        evt.Valor.Should().Be(valor);
    }

    [Fact]
    public void Create_ComOverride_DeveRespeitarCorrelationIdECausationIdETipo()
    {
        var orderId       = Guid.NewGuid();
        var cliente       = "Ana";
        var produto       = "Pix";
        var valor         = 150m;
        var correlationId = Guid.NewGuid().ToString("N");
        var causationId   = Guid.NewGuid().ToString("N");
        var type          = "OrderCreated.custom";

        var evt = OrderCreatedIntegrationEvent.Create(
            orderId: orderId,
            cliente: cliente,
            produto: produto,
            valor: valor,
            correlationId: correlationId,
            causationId: causationId,
            type: type
        );

        evt.Type.Should().Be(type);
        evt.CorrelationId.Should().Be(correlationId);
        evt.CausationId.Should().Be(causationId);

        evt.OrderId.Should().Be(orderId);
        evt.Cliente.Should().Be(cliente);
        evt.Produto.Should().Be(produto);
        evt.Valor.Should().Be(valor);
    }

    [Fact]
    public void Create_MultiplasChamadasDevemGerarIdsUnicos()
    {

        var orderId = Guid.NewGuid();

        var e1 = OrderCreatedIntegrationEvent.Create(orderId, "A", "X", 10m);
        var e2 = OrderCreatedIntegrationEvent.Create(orderId, "A", "X", 10m);

        e1.Id.Should().NotBeEmpty();
        e2.Id.Should().NotBeEmpty();
        e1.Id.Should().NotBe(e2.Id, "cada evento deve ter um Id único");
    }
}
