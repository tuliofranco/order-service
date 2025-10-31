using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Core.Application.Abstractions;

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
                _logger.LogInformation("UoW commit {Count} entries", entries.Count);

                var rows = await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);

                _logger.LogInformation("UoW commit ok ({Rows} rows)", rows);
                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UoW commit failed, rollback");
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
}
