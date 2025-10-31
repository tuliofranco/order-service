using Order.Core.Domain.Entities;

namespace Order.Core.Application.Abstractions.Repositories;
public interface IOrderStatusHistoryRepository
{
    Task AddAsync(OrderStatusHistory entry, CancellationToken ct = default);

    Task<IReadOnlyList<OrderStatusHistory>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken ct = default);

    Task<bool> ExistsByEventIdAsync(string eventId, CancellationToken ct = default);
}
