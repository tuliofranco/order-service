using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Domain.Repositories;
using Order.Core.Abstractions;
using Order.Core.Abstractions.Messaging.Outbox;
using Order.Core.Events;

namespace Order.Core.Services;


public class OrderService(
    IOrderRepository repo,
    IUnitOfWork uow,
    IOutboxStore outbox
) : IOrderService
{
    private readonly IOrderRepository _repo = repo;
    private readonly IUnitOfWork _uow = uow;
    private readonly IOutboxStore _outbox = outbox;

    public async Task<OrderEntity> CreateOrderAsync(
        string clienteNome,
        string produto,
        decimal valor,
        CancellationToken ct = default)
    {
        var order = OrderEntity.Create(clienteNome, produto, valor);

        await _repo.AddAsync(order, ct);

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
}