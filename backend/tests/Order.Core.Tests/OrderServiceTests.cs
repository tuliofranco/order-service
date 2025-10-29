using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Order.Core.Abstractions;
using Order.Core.Domain.Entities;
using Order.Core.Domain.Repositories;
using Order.Core.Events;
using Order.Core.Services;
using Order.Core.Enums;
using DomainOrder = Order.Core.Domain.Entities.Order;

namespace Order.Core.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_DeveCriarSalvarPublicarERetornarAOrder()
    {
        var repoMock = new Mock<IOrderRepository>();
        var publisherMock = new Mock<IEventPublisher>();

        var service = new OrderService(repoMock.Object, publisherMock.Object);

        var ct = CancellationToken.None;

        var nome = "Tulio";
        var produto = "Boleto";
        var valor = 300m;

        var created = await service.CreateOrderAsync(nome, produto, valor, ct);

        // assert 1: retorno da service
        created.Should().NotBeNull();
        created.ClienteNome.Should().Be(nome);
        created.Produto.Should().Be(produto);
        created.Valor.Should().Be(valor);
        created.Status.Should().Be(OrderStatus.Pendente);
        created.Id.Should().NotBe(Guid.Empty);

        // assert 2: o serviço chamou o repositório pra persistir exatamente essa ordem
        repoMock.Verify(r =>
            r.AddAsync(
                It.Is<DomainOrder>(o => o.Id == created.Id),
                ct),
            Times.Once,
            "CreateOrderAsync precisa persistir a ordem criada");

        // assert 3: o serviço publicou um evento OrderCreatedEvent com o mesmo Id
        publisherMock.Verify(p =>
            p.PublishAsync(
                It.Is<OrderCreatedEvent>(evt =>
                    evt.OrderId == created.Id &&
                    evt.CorrelationId == created.Id &&
                    evt.EventType == "OrderCreated"
                ),
                ct),
            Times.Once,
            "CreateOrderAsync precisa publicar OrderCreatedEvent com o Id correto");
    }

    [Fact]
    public async Task GetAllAsync_DeveRetornarTodasAsOrdersDoRepositorio()
    {
        // arrange
        var repoMock = new Mock<IOrderRepository>();
        var publisherMock = new Mock<IEventPublisher>();

        var expectedList = new List<DomainOrder>
        {
            DomainOrder.Create("Tulio", "Boleto", 300m),
            DomainOrder.Create("Ana", "Pix", 150m)
        };

        repoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        var service = new OrderService(repoMock.Object, publisherMock.Object);

        var ct = CancellationToken.None;


        var result = await service.GetAllAsync(ct);

        result.Should().BeSameAs(expectedList, "o service só delega GetAllAsync pro repo");

        repoMock.Verify(r => r.GetAllAsync(ct), Times.Once);
        publisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_DeveRetornarOrderCorretaDoRepositorio()
    {
        // arrange
        var repoMock = new Mock<IOrderRepository>();
        var publisherMock = new Mock<IEventPublisher>();

        var existingOrder = DomainOrder.Create("Tulio", "Boleto", 300m);
        var orderId = existingOrder.Id;

        repoMock
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        var service = new OrderService(repoMock.Object, publisherMock.Object);

        var ct = CancellationToken.None;

        // act
        var result = await service.GetByIdAsync(orderId, ct);

        // assert
        result.Should().Be(existingOrder);

        repoMock.Verify(r => r.GetByIdAsync(orderId, ct), Times.Once);
        publisherMock.VerifyNoOtherCalls();
    }
}
