using OrderEntity = Order.Core.Domain.Entities.Order;
namespace Order.Core.Services;

public interface IOrderService
{
    Task<OrderEntity> CreateOrderAsync(
        string clienteNome,
        string produto,
        decimal valor,
        CancellationToken ct = default);

    Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<OrderEntity>> GetAllAsync(CancellationToken ct = default);
}
