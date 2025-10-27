using Microsoft.EntityFrameworkCore;
using OrderService.Core.Domain.Entities;
using OrderService.Core.Domain.Repositories;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _ctx;

    public OrderRepository(OrderDbContext ctx) => _ctx = ctx;

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _ctx.Order.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _ctx.Order.AddAsync(order, ct);
    }

    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _ctx.Order.Update(order);
        return Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync(CancellationToken ct = default)
        => await _ctx.SaveChangesAsync(ct) > 0;

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
        => await _ctx.Order
            .AsNoTracking()
            .OrderByDescending(o => o.DataCriacaoUtc)
            .ToListAsync(ct);
}
