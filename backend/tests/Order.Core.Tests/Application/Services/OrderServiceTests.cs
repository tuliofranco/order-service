using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Order.Core.Application.Abstractions;
using Order.Core.Application.Abstractions.Messaging.Outbox;
using Order.Core.Application.Abstractions.Repositories;
using Order.Core.Application.Services;
using Order.Core.Domain.Entities.Enums;
using Order.Core.Domain.Events;


using DomainOrder = Order.Core.Domain.Entities.Order;
using OrderStatusHistoryEntity = Order.Core.Domain.Entities.OrderStatusHistory;

namespace Order.Core.Tests.Application.Services;

public class OrderServiceTests
{

    [Fact]
    public async Task CreateOrderAsync_DevePersistir_Historizar_EnfileirarOutbox_E_Comitar()
    {
        // arrange
        var repo        = new Mock<IOrderRepository>();
        var uow         = new Mock<IUnitOfWork>();
        var historyRepo = new Mock<IOrderStatusHistoryRepository>();
        var outbox      = new Mock<IOutboxStore>();
        var logger = NullLogger<OrderService>.Instance;

        var service = new OrderService(
            repo.Object,
            uow.Object,
            historyRepo.Object,
            outbox.Object,
            logger
        );

        var ct      = CancellationToken.None;
        var cliente = "Tulio";
        var produto = "Boleto";
        var valor   = 300m;

        DomainOrder? capturedOrder = null;
        repo.Setup(r => r.AddAsync(It.IsAny<DomainOrder>(), ct))
            .Callback<DomainOrder, CancellationToken>((o, _) => capturedOrder = o)
            .Returns(Task.CompletedTask);

        var created = await service.CreateOrderAsync(cliente, produto, valor, ct);

        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.ClienteNome.Should().Be(cliente);
        created.Produto.Should().Be(produto);
        created.Valor.Should().Be(valor);
        created.Status.Should().Be(OrderStatus.Pendente, "o pedido nasce pendente");

        repo.Verify(r => r.AddAsync(
            It.Is<DomainOrder>(o => o.Id == created.Id
                                    && o.ClienteNome == cliente
                                    && o.Produto == produto
                                    && o.Valor == valor
                                    && o.Status == OrderStatus.Pendente),
            ct), Times.Once);

        historyRepo.Verify(h => h.AddAsync(
            It.Is<OrderStatusHistoryEntity>(h =>
                h.OrderId == created.Id
                && h.FromStatus == null
                && h.ToStatus == OrderStatus.Pendente
                && h.Source == "Api"
                && h.CorrelationId == created.Id.ToString()),
            ct), Times.Once);

        outbox.Verify(o => o.AppendAsync(
            It.Is<IIntegrationEvent>(e =>
                e != null
                && e.GetType() == typeof(OrderCreatedIntegrationEvent)
                && ((OrderCreatedIntegrationEvent)e).OrderId == created.Id
                && ((OrderCreatedIntegrationEvent)e).Cliente == created.ClienteNome
                && ((OrderCreatedIntegrationEvent)e).Produto == created.Produto
                && ((OrderCreatedIntegrationEvent)e).Valor == created.Valor
                && ((OrderCreatedIntegrationEvent)e).Type == "OrderCreated"
                && ((OrderCreatedIntegrationEvent)e).CorrelationId == created.Id.ToString()
            ),
            ct), Times.Once);

        uow.Verify(u => u.CommitAsync(ct), Times.Once);


        repo.VerifyNoOtherCalls();
        historyRepo.VerifyNoOtherCalls();
        outbox.VerifyNoOtherCalls();
        uow.VerifyNoOtherCalls();

    }

    [Fact]
    public async Task GetAllAsync_DeveDelegarAoRepositorioERetornarMesmaLista()
    {
        var repo        = new Mock<IOrderRepository>();
        var uow         = new Mock<IUnitOfWork>();
        var historyRepo = new Mock<IOrderStatusHistoryRepository>();
        var outbox      = new Mock<IOutboxStore>();
        var logger      = NullLogger<OrderService>.Instance;

        var service = new OrderService(
            repo.Object,
            uow.Object,
            historyRepo.Object,
            outbox.Object,
            logger
        );

        var expected = new List<DomainOrder>
        {
            DomainOrder.Create("Tulio", "Boleto", 300m),
            DomainOrder.Create("Ana", "Pix", 150m)
        };

        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var ct = CancellationToken.None;

        var result = await service.GetAllAsync(ct);

        result.Should().BeSameAs(expected);
        repo.Verify(r => r.GetAllAsync(ct), Times.Once);
        uow.VerifyNoOtherCalls();
        historyRepo.VerifyNoOtherCalls();
        outbox.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_DeveRetornarDoRepositorio_QuandoExistir()
    {
        var repo        = new Mock<IOrderRepository>();
        var uow         = new Mock<IUnitOfWork>();
        var historyRepo = new Mock<IOrderStatusHistoryRepository>();
        var outbox      = new Mock<IOutboxStore>();
        var logger      = NullLogger<OrderService>.Instance;

        var service = new OrderService(
            repo.Object,
            uow.Object,
            historyRepo.Object,
            outbox.Object,
            logger
        );

        var existing = DomainOrder.Create("Tulio", "Boleto", 300m);
        var id = existing.Id;

        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var ct = CancellationToken.None;

        var result = await service.GetByIdAsync(id, ct);
        result.Should().Be(existing);
        repo.Verify(r => r.GetByIdAsync(id, ct), Times.Once);
        uow.VerifyNoOtherCalls();
        historyRepo.VerifyNoOtherCalls();
        outbox.VerifyNoOtherCalls();
    }
}
