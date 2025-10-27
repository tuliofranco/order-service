using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Domain.Repositories;
using Order.Core.Enums;
using Order.Core.Services;

namespace Order.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;

    public OrderService(IOrderRepository repo)
    {
        _repo = repo;
    }

    public async Task<OrderEntity> CreateOrderAsync(
        string clienteNome,
        string produto,
        decimal valor,
        CancellationToken ct = default)
    {
        // cria entidade com estado inicial Pendente
        var order = OrderEntity.Create(clienteNome, produto, valor);

        await _repo.AddAsync(order, ct);

        // aqui no futuro: publicar OrderCreatedEvent no Service Bus
        // (CorrelationId = order.Id, EventType = "OrderCreated")

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
