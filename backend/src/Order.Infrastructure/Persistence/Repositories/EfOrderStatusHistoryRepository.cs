using Microsoft.EntityFrameworkCore;
using Order.Core.Domain.Entities;
using Order.Core.Application.Abstractions.Repositories;

namespace Order.Infrastructure.Persistence.Repositories;

public sealed class EfOrderStatusHistoryRepository : IOrderStatusHistoryRepository
{
    private readonly OrderDbContext _db;

    public EfOrderStatusHistoryRepository(OrderDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(OrderStatusHistory entry, CancellationToken ct = default)
    {
        await _db.OrderStatusHistories.AddAsync(entry, ct);
    }

    public async Task<IReadOnlyList<OrderStatusHistory>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await _db.OrderStatusHistories
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.OccurredAt)
            .ToListAsync(ct);
    }

    public Task<bool> ExistsByEventIdAsync(string eventId, CancellationToken ct = default)
    {
        return _db.OrderStatusHistories.AnyAsync(x => x.EventId == eventId.ToString(), ct);
    }
}
