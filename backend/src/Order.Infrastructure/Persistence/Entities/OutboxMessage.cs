namespace Order.Infrastructure.Persistence.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime OccurredOnUtc { get; set; }
}
