using Microsoft.EntityFrameworkCore;
using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Domain.Repositories;
using Order.Infrastructure.Persistence;

namespace Order.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _ctx;

    public OrderRepository(OrderDbContext ctx) => _ctx = ctx;

    public async Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Order.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(OrderEntity order, CancellationToken ct = default)
    {
        await _ctx.Order.AddAsync(order, ct);
    }

    public Task UpdateAsync(OrderEntity order, CancellationToken ct = default)
    {
        _ctx.Order.Update(order);
        return Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken ct = default)
        => await _ctx.SaveChangesAsync(ct) > 0;

    public async Task<IReadOnlyList<OrderEntity>> GetAllAsync(CancellationToken ct = default)
        => await _ctx.Order
            .AsNoTracking()
            .OrderByDescending(o => o.DataCriacaoUtc)
            .ToListAsync(ct);
}
