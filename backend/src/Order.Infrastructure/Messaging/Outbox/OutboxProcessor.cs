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
        _logger.LogInformation("OutboxProcessor iniciado (tamanho do lote = {Batch})", _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await _store.FetchPendingBatchAsync(_batchSize, stoppingToken);

                if (batch.Count == 0)
                {
                    await Task.Delay(_idleDelay, stoppingToken);
                    continue;
                }

                foreach (var record in batch)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    using (_logger.BeginScope(new Dictionary<string, object?>
                    {
                        ["outboxId"] = record.Id,
                        ["eventType"] = record.Type
                    }))
                    {
                        _logger.LogInformation("Publicando mensagem do outbox");

                        PublishResult result;
                        try
                        {
                            result = await _publisher.PublishAsync(record, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro inesperado durante publish");
                            result = PublishResult.RetryableFailure;
                        }

                        switch (result)
                        {
                            case PublishResult.Success:
                                await _store.MarkPublishedAsync(record.Id, stoppingToken);
                                _logger.LogInformation("Mensagem publicada e removida do outbox");
                                break;

                            case PublishResult.RetryableFailure:
                                _logger.LogWarning("Falha temporária ao publicar (vai tentar novamente)");
                                await Task.Delay(_errorDelay, stoppingToken);
                                break;

                            case PublishResult.PermanentFailure:
                                _logger.LogError("Falha permanente ao publicar (mensagem mantida para análise)");
                                await Task.Delay(_errorDelay, stoppingToken);
                                break;
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no loop principal do OutboxProcessor");
                await Task.Delay(_errorDelay, stoppingToken);
            }
        }

        _logger.LogInformation("OutboxProcessor encerrando");
    }
}
