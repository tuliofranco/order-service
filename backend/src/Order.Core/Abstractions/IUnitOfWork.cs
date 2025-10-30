
namespace Order.Core.Abstractions;

public interface IUnitOfWork
{

    Task<int> CommitAsync(CancellationToken ct = default);
}
