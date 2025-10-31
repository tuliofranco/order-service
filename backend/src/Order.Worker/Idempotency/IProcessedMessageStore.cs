namespace Order.Worker.Idempotency;

public interface IProcessedMessageStore
{
    Task<bool> HasProcessedAsync(string messageId, CancellationToken ct);
    Task<bool> TryMarkProcessedAsync(string messageId, CancellationToken ct);
}
