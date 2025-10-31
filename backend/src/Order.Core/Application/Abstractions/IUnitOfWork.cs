
namespace Order.Core.Application.Abstractions;

public interface IUnitOfWork
{

    Task<int> CommitAsync(CancellationToken ct = default);
}
