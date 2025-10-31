namespace Order.Worker.Idempotency;

public interface IProcessedMessageStore
{
    Task<bool> HasProcessedAsync(string messageId, CancellationToken ct);

    /// <summary>
    /// Marca como processada. Retorna true se inseriu (primeira vez),
    /// false se já existia (duplicata). Deve ser chamado DENTRO da transação.
    /// </summary>
    Task<bool> TryMarkProcessedAsync(string messageId, CancellationToken ct);
}
