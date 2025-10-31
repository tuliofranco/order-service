using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Application.Abstractions.Repositories;

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

    public async Task<bool> MarkProcessingIfPendingAsync(Guid orderId, CancellationToken ct = default)
    {
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE orders
            SET status = {"Processando"}
            WHERE id = {orderId}
              AND status = {"Pendente"};
        ", ct);

        if (rows == 1)
        {
            _logger.LogInformation("Order {OrderId} setado como Processando", orderId);
            return true;
        }

        _logger.LogInformation("Order {OrderId} não mudou para o status de Processando", orderId);
        return false;
    }

    public async Task<bool> MarkFinalizedIfProcessingAsync(Guid orderId, CancellationToken ct = default)
    {
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE orders
            SET status = {"Finalizado"}
            WHERE id = {orderId}
              AND status = {"Processando"};
        ", ct);

        if (rows == 1)
        {
            _logger.LogInformation("Order {OrderId} setado como Finalizado.", orderId);
            return true;
        }

        _logger.LogInformation("Order {OrderId} não mudou para o status de Finalizado.", orderId);
        return false;
    }

    public Task<bool> ExistsAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Orders.AnyAsync(o => o.Id == orderId, ct);
}
