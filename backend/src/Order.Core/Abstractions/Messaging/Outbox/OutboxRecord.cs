namespace Order.Core.Abstractions.Messaging.Outbox;

// Registro que o OutboxStore devolve para processamento/publicação.
public sealed record OutboxRecord(
    Guid Id,
    string Type,
    string Payload,
    DateTime OccurredOnUtc,
    string? CorrelationId,
    int Attempts
);

public enum PublishResult
{
    Success,
    RetryableFailure, // Falha transitória (tentar novamente)
    PermanentFailure  // Falha definitiva (não tentar de novo)
}
