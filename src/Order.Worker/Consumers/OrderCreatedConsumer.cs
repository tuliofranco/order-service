using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Worker.Services;

namespace Order.Worker.Consumers
{
    public sealed class OrderCreatedConsumer : BackgroundService
    {
        private readonly ILogger<OrderCreatedConsumer> _logger;
        private readonly ServiceBusClient _busClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _queueName;
        private ServiceBusProcessor? _processor;

        public OrderCreatedConsumer(
            ILogger<OrderCreatedConsumer> logger,
            ServiceBusClient busClient,
            IServiceScopeFactory scopeFactory,
            string queueName)
        {
            _logger = logger;
            _busClient = busClient;
            _scopeFactory = scopeFactory;
            _queueName = queueName;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando consumidor da fila {Queue}...", _queueName);

            _processor = _busClient.CreateProcessor(_queueName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            });

            _processor.ProcessMessageAsync += HandleMessageAsync;
            _processor.ProcessErrorAsync   += HandleErrorAsync;

            await _processor.StartProcessingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("Parando consumidor da fila {Queue}...", _queueName);
            await _processor.StopProcessingAsync(stoppingToken);
        }

        private async Task HandleMessageAsync(ProcessMessageEventArgs args)
        {
            string correlationId = args.Message.CorrelationId;
            string body = args.Message.Body.ToString();

            _logger.LogInformation(
                "Mensagem recebida. CorrelationId={CorrelationId} Body={Body}",
                correlationId,
                body
            );

            OrderCreatedPayload? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<OrderCreatedPayload>(body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao desserializar payload da mensagem");
            }

            if (payload == null || payload.OrderId == Guid.Empty)
            {
                _logger.LogWarning("Payload inválido. DLQ.");
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "InvalidPayload",
                    "Body não desserializou corretamente para OrderId."
                );
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var updater = scope.ServiceProvider.GetRequiredService<StatusUpdater>();

                await updater.AdvanceOrderStatusAsync(
                    payload.OrderId,
                    args.CancellationToken
                );
            }

            // Se processou com sucesso, retira a mensagem da fila
            await args.CompleteMessageAsync(args.Message);
        }
        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(
                args.Exception,
                "Erro no Service Bus. EntityPath={EntityPath} Source={ErrorSource}",
                args.EntityPath,
                args.ErrorSource
            );
            return Task.CompletedTask;
        }
        private sealed class OrderCreatedPayload
        {
            public Guid OrderId { get; set; }
        }
    }
}
