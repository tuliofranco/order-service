namespace Order.Core.Abstractions.Messaging.Outbox;

public interface IOutboxStore
{
    // Grava o evento no outbox participando da MESMA transação do agregado.
    Task AppendAsync(IIntegrationEvent @event, CancellationToken ct = default);

    Task<IReadOnlyList<OutboxRecord>> FetchPendingBatchAsync(
        int maxBatchSize,
        CancellationToken ct = default
    );

    Task MarkPublishedAsync(Guid outboxId, CancellationToken ct = default);

    Task MarkFailedAsync(Guid outboxId, string error, CancellationToken ct = default);
}
