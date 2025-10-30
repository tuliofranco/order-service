using System;
using System.Text.Json.Serialization;

namespace Order.Worker.Consumers;

public sealed class OrderCreatedPayload
{
    // Campos comuns do seu IIntegrationEvent (opcionais para o processamento atual)
    public Guid Id { get; init; }
    public DateTime OccurredOnUtc { get; init; }
    public string Type { get; init; } = default!;

    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; init; }

    // Campo necessário para o processamento
    [JsonPropertyName("orderId")]
    public Guid OrderId { get; init; }

    // Outros campos de negócio (opcionais para logs/diagnóstico)
    public string? Cliente { get; init; }
    public string? Produto { get; init; }
    public decimal? Valor { get; init; }
}
