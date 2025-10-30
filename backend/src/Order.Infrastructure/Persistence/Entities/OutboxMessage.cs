namespace Order.Infrastructure.Persistence.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }                  // PK
    public string Type { get; set; } = default!;  // Nome do evento
    public string Payload { get; set; } = default!; // JSON do evento
    public DateTime OccurredOnUtc { get; set; }   // Para ordenar (FIFO “suficiente”)
}
