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
        return await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<IReadOnlyList<OrderEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.data_criacao)
            .ToListAsync(ct);
    }

    public async Task AddAsync(OrderEntity order, CancellationToken ct = default)
    {
        await _db.Orders.AddAsync(order, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OrderEntity order, CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> MarkProcessingIfPendingAsync(Guid orderId, CancellationToken ct)
    {
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE orders
            SET status = {"Processando"}
            WHERE id = {orderId}
              AND status = {"Pendente"};
        ", ct);

        return rows == 1;
    }

    public Task<bool> ExistsAsync(Guid orderId, CancellationToken ct)
        => _db.Orders.AnyAsync(o => o.Id == orderId, ct);
}
