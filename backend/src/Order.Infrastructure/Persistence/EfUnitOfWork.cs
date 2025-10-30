
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Core.Abstractions;

namespace Order.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly OrderDbContext _db;
    private readonly ILogger<EfUnitOfWork> _logger;

    public EfUnitOfWork(OrderDbContext db, ILogger<EfUnitOfWork> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                var entries = _db.ChangeTracker.Entries().ToList();
                var states = string.Join(",",
                    entries.Select(e => $"{e.Entity.GetType().Name}:{e.State}"));

                _logger.LogInformation(
                    "UoW BEFORE | DbHash={DbHash} | Entries={Count} | States={States}",
                    _db.GetHashCode(),
                    entries.Count,
                    states
                );

                var rows = await _db.SaveChangesAsync(ct);

                _logger.LogInformation("UoW AFTER  | rows={Rows}", rows);

                await tx.CommitAsync(ct);
                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UoW ERROR  | rollback triggered");
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
}
