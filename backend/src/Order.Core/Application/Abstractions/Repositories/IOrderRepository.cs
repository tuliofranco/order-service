using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Domain.Entities.Enums;

namespace Order.Core.Application.Abstractions.Repositories;

public interface IOrderRepository
{
    Task AddAsync(OrderEntity order, CancellationToken ct = default);

    Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<OrderEntity>> GetAllAsync(CancellationToken ct = default);

    Task UpdateAsync(OrderEntity order, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid orderId, CancellationToken ct);

    Task<bool> ChangeStatusAsync(Guid orderId, OrderStatus from, OrderStatus to, CancellationToken ct = default);
}
