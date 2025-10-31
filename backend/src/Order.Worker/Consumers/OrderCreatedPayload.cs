using System.Text.Json.Serialization;

namespace Order.Worker.Consumers;

public sealed class OrderCreatedPayload
{
    public Guid Id { get; init; }
    public DateTime OccurredOnUtc { get; init; }
    public string Type { get; init; } = default!;

    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; init; }

    [JsonPropertyName("orderId")]
    public Guid OrderId { get; init; }

    public string? Cliente { get; init; }
    public string? Produto { get; init; }
    public decimal? Valor { get; init; }
}
