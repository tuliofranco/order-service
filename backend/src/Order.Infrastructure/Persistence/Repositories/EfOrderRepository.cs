using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Application.Abstractions.Repositories;
using Order.Core.Domain.Entities.Enums;
using Order.Core.Domain.Rules;

namespace Order.Infrastructure.Persistence.Repositories;

public class EfOrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;
    private readonly ILogger<EfOrderRepository> _logger;

    public EfOrderRepository(OrderDbContext db, ILogger<EfOrderRepository> logger)
    {
        _db = db;
        _logger = logger;
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
        _logger.LogInformation("Insert order {OrderId}", order.Id);
        await _db.Orders.AddAsync(order, ct);
    }

    public Task UpdateAsync(OrderEntity order, CancellationToken ct = default)
    {
        _logger.LogInformation("Update order {OrderId}", order.Id);
        _db.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Orders.AnyAsync(o => o.Id == orderId, ct);


    public async Task<bool> ChangeStatusAsync(Guid orderId, OrderStatus from, OrderStatus to, CancellationToken ct = default)
    {
        OrderStatusTransitionValidator.EnsureValid(from, to);

        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE orders
               SET status = {to.ToString()}
             WHERE id     = {orderId}
               AND status = {from.ToString()};
        ", ct);

        if (rows == 1)
        {
            _logger.LogInformation("Order {OrderId}: {From} -> {To}", orderId, from, to);
            return true;
        }

        _logger.LogInformation("Order {OrderId}: transição ignorada ({From} -> {To})", orderId, from, to);
        return false;
    }

}
