// backend/src/Order.Infrastructure/Persistence/EfOrderRepository.cs
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

    public Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<OrderEntity>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Orders
                 .AsNoTracking()
                 .OrderByDescending(o => o.data_criacao)
                 .ToListAsync(ct);

    public async Task AddAsync(OrderEntity order, CancellationToken ct = default)
    {
        await _db.Orders.AddAsync(order, ct);
    }

    public Task UpdateAsync(OrderEntity order, CancellationToken ct = default)
    {
        _db.Orders.Update(order);
        return Task.CompletedTask;
    }
    public async Task<bool> MarkProcessingIfPendingAsync(Guid orderId, CancellationToken ct = default)
    {
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE orders
            SET status = {"Processando"}
            WHERE id = {orderId}
              AND status = {"Pendente"};
        ", ct);

        return rows == 1;
    }

    public Task<bool> ExistsAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Orders.AnyAsync(o => o.Id == orderId, ct);
}
