using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Domain.Repositories;
using Order.Core.Abstractions;
using Order.Core.Events;


namespace Order.Core.Services;

public class OrderService(IOrderRepository repo, IEventPublisher publisher) : IOrderService
{
    private readonly IOrderRepository _repo = repo;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<OrderEntity> CreateOrderAsync(
        string clienteNome,
        string produto,
        decimal valor,
        CancellationToken ct = default)
    {
        var order = OrderEntity.Create(clienteNome, produto, valor);

        await _repo.AddAsync(order, ct);

        var evt = OrderCreatedEvent.FromOrderId(order.Id);
        //await _publisher.PublishSameEventNTimesAsync(evt, 5, ct);
        await _publisher.PublishAsync(evt, ct);
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
