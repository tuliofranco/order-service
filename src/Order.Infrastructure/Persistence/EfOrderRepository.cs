using Microsoft.EntityFrameworkCore;
using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Domain.Repositories;

namespace Order.Infrastructure.Persistence;

public class EfOrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;

    public EfOrderRepository(OrderDbContext db)
    {
        _db = db;
    }

    public async Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Order
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<IReadOnlyList<OrderEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Order
            .AsNoTracking()
            .OrderByDescending(o => o.DataCriacaoUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(OrderEntity order, CancellationToken ct = default)
    {
        await _db.Order.AddAsync(order, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OrderEntity order, CancellationToken ct = default)
    {
        _db.Order.Update(order);
        await _db.SaveChangesAsync(ct);
    }
}
