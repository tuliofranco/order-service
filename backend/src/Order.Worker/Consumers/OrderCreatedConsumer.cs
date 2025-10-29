#nullable enable

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Persistence;
using orderEnum = Order.Core.Enums.OrderStatus;
using Order.Worker.Services;
using Order.Core.Domain.Repositories;

namespace Order.Worker.Consumers;

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
            MaxConcurrentCalls = 3,
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
        var correlationId = args.Message.CorrelationId;
        var messageId     = args.Message.MessageId;
        var body          = args.Message.Body.ToString();

        _logger.LogInformation(
            "Mensagem recebida. CorrelationId={CorrelationId} MessageId={MessageId} Body={Body}",
            correlationId, messageId, body
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

        if (payload is null || payload.OrderId == Guid.Empty)
        {
            _logger.LogWarning("Payload inválido. DLQ.");
            await args.DeadLetterMessageAsync(
                args.Message, "InvalidPayload",
                "Body não desserializou corretamente para OrderId."
            );
            return;
        }
        await ProcessOrderAsync(payload.OrderId, messageId, args.CancellationToken);
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


    public async Task ProcessOrderAsync(Guid orderId, string messageId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db      = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var repo    = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        // status -> Processando
        var changed = await repo.MarkProcessingIfPendingAsync(orderId, ct);

        if (!changed)
        {
            var exists = await repo.ExistsAsync(orderId, ct);
            if (!exists)
            {
                _logger.LogWarning("Pedido {OrderId} não encontrado.", orderId);
                return;
            }

            _logger.LogInformation("Pedido {OrderId} já não está Pendente (ignorado).", orderId);
        }

        // Deley de 5 sec
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var rows = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO processed_messages (message_id, processed_at_utc)
                VALUES ({messageId}, {DateTime.UtcNow})
                ON CONFLICT (message_id) DO NOTHING;
            ", ct);

            if (rows == 0)
            {
                await tx.RollbackAsync(ct);
                _logger.LogInformation("Duplicata detectada. MessageId={MessageId}. Ignorando.", messageId);
                return;
            }

            var order = await db.Orders.FindAsync(new object?[] { orderId }, ct);
            if (order is null)
            {
                await tx.RollbackAsync(ct);
                _logger.LogWarning("Pedido {OrderId} não encontrado na etapa final.", orderId);
                return;
            }

            if (order.Status != orderEnum.Finalizado)
            {
                order.Status = orderEnum.Finalizado;
                _logger.LogInformation("Pedido {OrderId} marcado como Finalizado.", orderId);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao finalizar MessageId={MessageId}.", messageId);
            await tx.RollbackAsync(ct);
        }
    }

}
