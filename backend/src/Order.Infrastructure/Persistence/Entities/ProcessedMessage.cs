namespace Order.Infrastructure.Persistence.Entities;
public sealed class ProcessedMessage
{
    public string MessageId { get; set; } = default!;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
