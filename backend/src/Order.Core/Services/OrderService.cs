using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Domain.Repositories;
using Order.Core.Abstractions;
using Order.Core.Abstractions.Messaging.Outbox;
using Order.Core.Events;
using Order.Core.Domain.Validation;
using Order.Core.Domain.Entities.Enums;
using Order.Core.Domain.Entities;
namespace Order.Core.Services;


public class OrderService(
    IOrderRepository repo,
    IUnitOfWork uow,
    IOrderStatusHistoryRepository historyRepo,
    IOutboxStore outbox
) : IOrderService
{
    private readonly IOrderRepository _repo = repo;
    private readonly IUnitOfWork _uow = uow;
    private readonly IOrderStatusHistoryRepository _historyRepo = historyRepo;
    private readonly IOutboxStore _outbox = outbox;

    public async Task<OrderEntity> CreateOrderAsync(
        string clienteNome,
        string produto,
        decimal valor,
        CancellationToken ct = default)
    {
        var order = OrderEntity.Create(clienteNome, produto, valor);
        OrderStatusTransitionValidator.EnsureValid(null, OrderStatus.Pendente);

        var integrationEvent = new OrderCreatedIntegrationEvent(
            Id: Guid.NewGuid(),
            OccurredOnUtc: DateTime.UtcNow,
            Type: "OrderCreated.v1",
            CorrelationId: order.Id.ToString(),
            CausationId: null,
            OrderId: order.Id,
            Cliente: clienteNome,
            Produto: produto,
            Valor: valor
        );

        await _repo.AddAsync(order, ct);

        var historyEntry = OrderStatusHistory.Create(
            orderId: order.Id,
            fromStatus: null,
            toStatus: OrderStatus.Pendente,
            correlationId: order.Id.ToString(),
            eventId: integrationEvent.Id.ToString(), 
            source: "Api",
            reason: "Pedido criado");

        await _historyRepo.AddAsync(historyEntry, ct);



        await _outbox.AppendAsync(integrationEvent, ct);

        await _uow.CommitAsync(ct);

        return order;
    }
    public Task<IReadOnlyList<OrderEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return _repo.GetAllAsync(ct);
    }

    public Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _repo.GetByIdAsync(id, ct);
    }

    public Task<IReadOnlyList<OrderStatusHistory>> GetHistoryByOrderIdAsync(
        Guid orderId,
        CancellationToken ct = default)
        => _historyRepo.GetByOrderIdAsync(orderId, ct);
}