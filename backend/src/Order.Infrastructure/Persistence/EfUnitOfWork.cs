using Microsoft.EntityFrameworkCore;
using Order.Core.Abstractions;

namespace Order.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly OrderDbContext _dbContext;

    public EfUnitOfWork(OrderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                var affected = await _dbContext.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
                return affected;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
}
