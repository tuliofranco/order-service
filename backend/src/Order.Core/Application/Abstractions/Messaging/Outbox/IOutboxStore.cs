namespace Order.Core.Application.Abstractions.Messaging.Outbox;

public interface IOutboxStore
{
    Task AppendAsync(IIntegrationEvent @event, CancellationToken ct = default);

    Task<IReadOnlyList<OutboxRecord>> FetchPendingBatchAsync(
        int maxBatchSize,
        CancellationToken ct = default
    );

    Task MarkPublishedAsync(Guid outboxId, CancellationToken ct = default);

    Task MarkFailedAsync(Guid outboxId, string error, CancellationToken ct = default);
}
