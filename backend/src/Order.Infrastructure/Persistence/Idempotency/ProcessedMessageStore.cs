using Microsoft.EntityFrameworkCore;
using Order.Core.Application.Abstractions.Idempotency;
using Order.Infrastructure.Persistence.Entities;
using Order.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Order.Core.Application.Abstractions;
namespace Order.Infrastructure.Persistence.Idempotency;

public sealed class ProcessedMessageStore(OrderDbContext db, IServiceScopeFactory _scopeFactory) : IProcessedMessageStore
{
    public Task<bool> HasProcessedAsync(string messageId, CancellationToken ct = default)
        => db.ProcessedMessages.AnyAsync(x => x.MessageId == messageId, ct);

    public async Task<bool> TryMarkProcessedAsync(string messageId, CancellationToken ct = default)
    {

        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();


        var entity = new ProcessedMessage
        {
            MessageId = messageId,
            ProcessedAtUtc = DateTime.UtcNow
        };
        db.ProcessedMessages.Add(entity);

        try
        {
            await uow.CommitAsync();
            return true;
        }
        catch
        {
            return false;
        }
        

    }
}
