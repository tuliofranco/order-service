using OrderService.Core.Domain;

namespace OrderService.Core.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(
        string clienteNome,
        string produto,
        decimal valor,
        CancellationToken ct = default);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);
}
