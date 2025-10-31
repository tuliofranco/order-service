namespace Order.Core.Application.Abstractions.Messaging.Outbox;

// Registro que o OutboxStore devolve para processamento/publicação.
public sealed record OutboxRecord(
    Guid Id,
    string Type,
    string Payload,
    DateTime OccurredOnUtc
);

public enum PublishResult
{
    Success,
    RetryableFailure, // Falha transitória (tentar novamente)
    PermanentFailure  // Falha definitiva (não tentar de novo)
}
