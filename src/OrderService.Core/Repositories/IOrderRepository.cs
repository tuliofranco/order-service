using OrderService.Core.Domain;

namespace OrderService.Core.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);

    Task UpdateAsync(Order order, CancellationToken ct = default);
}
