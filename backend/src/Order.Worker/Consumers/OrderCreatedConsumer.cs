#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Infrastructure.Persistence;
using orderEnum = Order.Core.Domain.Entities.Enums;
using Order.Worker.Services;
using Order.Core.Domain.Repositories;
using Order.Core.Logging;
using Order.Core.Domain.Entities;

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
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync   += HandleErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        finally
        {
            _logger.LogInformation("Encerrando leitura da fila {Queue}.", _queueName);

            try
            {
                await _processor.StopProcessingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao parar o processor do Service Bus.");
            }

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
                await ProcessOrderAsync(payload.OrderId, messageId, correlationId, args.CancellationToken);
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

    public async Task ProcessOrderAsync(Guid orderId, string messageId, string? correlationId, CancellationToken ct)
    {
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["component"] = "Worker",
            ["evento"] = "ProcessarPedido",
            ["orderId"] = orderId.ToString(),
            ["correlationId"] = string.IsNullOrWhiteSpace(correlationId) ? orderId.ToString() : correlationId,
            [Correlation.Key] = string.IsNullOrWhiteSpace(correlationId) ? orderId.ToString() : correlationId
        }))
        {
            using var scope = _scopeFactory.CreateScope();
            var db          = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var repo        = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var historyRepo = scope.ServiceProvider.GetRequiredService<IOrderStatusHistoryRepository>();

            // 1. Atualiza para Processando (se ainda estava Pendente)
            var mudouParaProcessando = await repo.MarkProcessingIfPendingAsync(orderId, ct);

            if (mudouParaProcessando)
            {
                _logger.LogInformation(
                    "Pedido {OrderId} marcado como Processando.",
                    orderId
                );

                var historyProcessing = OrderStatusHistory.Create(
                    orderId: orderId,
                    fromStatus: orderEnum.OrderStatus.Pendente,
                    toStatus: orderEnum.OrderStatus.Processando,
                    correlationId: string.IsNullOrWhiteSpace(correlationId) ? orderId.ToString() : correlationId!,
                    eventId: messageId,
                    source: "Worker",
                    reason: "Status alterado para Processando"
                );

                await historyRepo.AddAsync(historyProcessing, ct);
            }
            else
            {
                var exists = await repo.ExistsAsync(orderId, ct);
                if (!exists)
                {
                    _logger.LogWarning("Pedido {OrderId} não encontrado.", orderId);
                    return;
                }

                _logger.LogInformation(
                    "Pedido {OrderId} já não estava Pendente. Nenhuma alteração.",
                    orderId
                );
            }

            // 2. Simula trabalho pesado
            await Task.Delay(TimeSpan.FromSeconds(5), ct);

            // 3. Transação final: marcar como Finalizado e registrar idempotência
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            try
            {
                // Tabela processed_messages garante idempotência
                var rows = await db.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO processed_messages (message_id, processed_at_utc)
                    VALUES ({messageId}, {DateTime.UtcNow})
                    ON CONFLICT (message_id) DO NOTHING;
                ", ct);

                if (rows == 0)
                {
                    // Já processado antes
                    await tx.RollbackAsync(ct);
                    _logger.LogInformation(
                        "Mensagem já processada anteriormente. MessageId={MessageId}",
                        messageId
                    );
                    return;
                }

                var order = await db.Orders.FindAsync(new object?[] { orderId }, ct);
                if (order is null)
                {
                    await tx.RollbackAsync(ct);
                    _logger.LogWarning(
                        "Pedido {OrderId} não encontrado para finalizar.",
                        orderId
                    );
                    return;
                }

                if (order.Status != orderEnum.OrderStatus.Finalizado)
                {
                    order.Status = orderEnum.OrderStatus.Finalizado;

                    _logger.LogInformation(
                        "Pedido {OrderId} marcado como Finalizado.",
                        orderId
                    );

                    var historyFinalized = OrderStatusHistory.Create(
                        orderId: order.Id,
                        fromStatus: orderEnum.OrderStatus.Processando,
                        toStatus:   orderEnum.OrderStatus.Finalizado,
                        correlationId: string.IsNullOrWhiteSpace(correlationId) ? order.Id.ToString() : correlationId!,
                        eventId: messageId,
                        source: "Worker",
                        reason: "Status alterado para Finalizado"
                    );

                    await historyRepo.AddAsync(historyFinalized, ct);
                }

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao finalizar pedido {OrderId} (MessageId={MessageId}).",
                    orderId,
                    messageId
                );

                await tx.RollbackAsync(ct);
            }
        }
    }
}