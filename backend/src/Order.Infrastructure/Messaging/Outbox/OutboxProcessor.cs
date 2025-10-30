
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Core.Abstractions.Messaging.Outbox;

namespace Order.Infrastructure.Messaging.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IOutboxStore _store;
    private readonly IOutboxPublisher _publisher;
    private readonly ILogger<OutboxProcessor> _logger;

    private readonly int _batchSize;
    private readonly TimeSpan _idleDelay;
    private readonly TimeSpan _errorDelay;

    public OutboxProcessor(
        IOutboxStore store,
        IOutboxPublisher publisher,
        ILogger<OutboxProcessor> logger,
        int batchSize = 50,
        int idleDelayMilliseconds = 1000,
        int errorDelayMilliseconds = 2000)
    {
        _store = store;
        _publisher = publisher;
        _logger = logger;

        _batchSize = batchSize <= 0 ? 50 : batchSize;
        _idleDelay = TimeSpan.FromMilliseconds(Math.Max(100, idleDelayMilliseconds));
        _errorDelay = TimeSpan.FromMilliseconds(Math.Max(200, errorDelayMilliseconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor iniciado. Lote={BatchSize}", _batchSize);
        

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await _store.FetchPendingBatchAsync(_batchSize, stoppingToken);
                _logger.LogInformation("Processor batch size = {Count}", batch.Count);


                if (batch.Count == 0)
                {
                    await Task.Delay(_idleDelay, stoppingToken);
                    continue;
                }

                foreach (var record in batch)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var result = await _publisher.PublishAsync(record, stoppingToken);

                    switch (result)
                    {
                        case PublishResult.Success:
                            await _store.MarkPublishedAsync(record.Id, stoppingToken);
                            _logger.LogDebug("Outbox {OutboxId} publicado e removido.", record.Id);
                            break;

                        case PublishResult.RetryableFailure:
                            // Modelo mínimo: manter registro para próxima tentativa.
                            _logger.LogWarning("Falha temporária ao publicar Outbox {OutboxId}. Tentará novamente.", record.Id);
                            // Pequeno atraso para evitar loop quente
                            await Task.Delay(_errorDelay, stoppingToken);
                            break;

                        case PublishResult.PermanentFailure:
                            // Modelo mínimo: não há coluna de erro; mantemos registro
                            // para análise manual ou futura extensão do schema.
                            _logger.LogError("Falha permanente ao publicar Outbox {OutboxId}. Mantendo registro para intervenção.", record.Id);
                            await Task.Delay(_errorDelay, stoppingToken);
                            break;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Encerrando graciosamente
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no loop do OutboxProcessor.");
                // Pequeno backoff global antes do próximo ciclo
                await Task.Delay(_errorDelay, stoppingToken);
            }
        }

        _logger.LogInformation("OutboxProcessor finalizado.");
    }
}
