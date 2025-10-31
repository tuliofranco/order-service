namespace Order.Core.Application.Abstractions.Idempotency;

public interface IProcessedMessageStore
{
    Task<bool> TryMarkProcessedAsync(string messageId, CancellationToken ct = default);
}
