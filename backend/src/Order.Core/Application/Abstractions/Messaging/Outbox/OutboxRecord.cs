namespace Order.Core.Application.Abstractions.Messaging.Outbox;

public sealed record OutboxRecord(
    Guid Id,
    string Type,
    string Payload,
    DateTime OccurredOnUtc
);

