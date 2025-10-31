using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;

namespace Order.Worker.Consumers;

public sealed class OrderCreatedConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _queueName;
    private ServiceBusProcessor? _processor;

    public OrderCreatedConsumer(
        ILogger<OrderCreatedConsumer> logger,
        IServiceScopeFactory scopeFactory,
        string? queueName)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _queueName = string.IsNullOrWhiteSpace(queueName)
            ? throw new ArgumentException("ASB entity name (queue/topic) não configurado. Defina ASB_ENTITY.", nameof(queueName))
            : queueName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado. Consumindo fila {Queue} do Service Bus.", _queueName);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var busClient = scope.ServiceProvider.GetRequiredService<ServiceBusClient>();

        _processor = busClient.CreateProcessor(_queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,          // paralelo
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync   += HandleErrorAsync;

        try
        {
            await _processor.StartProcessingAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _logger.LogInformation("Encerrando leitura da fila {Queue}.", _queueName);

            try { await _processor.StopProcessingAsync(CancellationToken.None); }
            catch (Exception ex) { _logger.LogWarning(ex, "Falha ao parar o processor( Service Bus )."); }

            await _processor.DisposeAsync();
        }
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var correlationId = args.Message.CorrelationId;
        var messageId     = args.Message.MessageId;
        var body          = args.Message.Body.ToString();

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["component"] = "Worker",
            ["evento"] = "MensagemRecebida",
            ["correlationId"] = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId,
            ["messageId"] = messageId
        }))
        {
            _logger.LogInformation(
                "Mensagem recebida. CorrelationId={CorrelationId} MessageId={MessageId}",
                correlationId,
                messageId
            );

            OrderCreatedPayload? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<OrderCreatedPayload>(body, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao desserializar o payload da mensagem.");
            }

            if (payload is null || payload.OrderId == Guid.Empty)
            {
                _logger.LogWarning("Payload inválido: OrderId vazio. Enviando para DLQ.");
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "InvalidPayload",
                    "Body não desserializou corretamente para OrderId."
                );
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<Processing.ProcessOrder>();

                await processor.ExecuteAsync(payload.OrderId, messageId, correlationId, args.CancellationToken);

                await args.CompleteMessageAsync(args.Message, args.CancellationToken);

                _logger.LogInformation(
                    "Processamento concluído. OrderId={OrderId} MessageId={MessageId}",
                    payload.OrderId,
                    messageId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar pedido. OrderId={OrderId} MessageId={MessageId}",
                    payload.OrderId,
                    messageId
                );

                if (args.Message.DeliveryCount >= 5)
                {
                    await args.DeadLetterMessageAsync(
                        args.Message,
                        "ProcessingFailed",
                        "Retries exceeded",
                        args.CancellationToken
                    );
                }
                else
                {
                    await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
                }
            }
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Erro recebido do Service Bus. EntityPath={EntityPath} Source={ErrorSource}",
            args.EntityPath,
            args.ErrorSource
        );
        return Task.CompletedTask;
    }

}
