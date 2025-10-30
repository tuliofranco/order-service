namespace Order.Core.Abstractions.Messaging.Outbox;

public interface IOutboxPublisher
{
    Task<PublishResult> PublishAsync(OutboxRecord record, CancellationToken ct = default);
}
