// backend/src/Order.Worker/Processing/ProcessOrder.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Core.Application.Abstractions;
using Order.Core.Application.Abstractions.Idempotency;
using Order.Core.Application.Abstractions.Repositories;
using Order.Core.Domain.Entities;
using Order.Infrastructure.Persistence;
using orderEnum = Order.Core.Domain.Entities.Enums;

namespace Order.Worker.Processing;

public sealed class ProcessOrder
{
    private readonly ILogger<ProcessOrder> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    // Se quiser, extraia para IOptions<WorkerOptions>
    private const int FinalizeDelaySeconds = 5;

    public ProcessOrder(ILogger<ProcessOrder> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(Guid orderId, string messageId, string? correlationId, CancellationToken ct)
    {
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["component"] = "Worker",
            ["evento"] = "ProcessarPedido",
            ["orderId"] = orderId.ToString(),
            ["correlationId"] = string.IsNullOrWhiteSpace(correlationId) ? orderId.ToString() : correlationId
        }))
        {
            using var scope        = _scopeFactory.CreateScope();
            var db                 = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var repo               = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var historyRepo        = scope.ServiceProvider.GetRequiredService<IOrderStatusHistoryRepository>();
            var idempotencyStore   = scope.ServiceProvider.GetRequiredService<IProcessedMessageStore>();
            var uow                = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // 1) Pendente → Processando (UPDATE condicional)
            var mudouParaProcessando = await repo.MarkProcessingIfPendingAsync(orderId, ct);
            if (mudouParaProcessando)
            {
                _logger.LogInformation("Pedido {OrderId} marcado como Processando.", orderId);

                var historyProcessing = OrderStatusHistory.Create(
                    orderId: orderId,
                    fromStatus: orderEnum.OrderStatus.Pendente,
                    toStatus:   orderEnum.OrderStatus.Processando,
                    correlationId: string.IsNullOrWhiteSpace(correlationId) ? orderId.ToString() : correlationId!,
                    eventId: messageId,
                    source: "Worker",
                    reason: "Status alterado para Processando"
                );

                await historyRepo.AddAsync(historyProcessing, ct);
                await uow.CommitAsync(ct); // persiste histórico “Processando”
            }
            else
            {
                var exists = await repo.ExistsAsync(orderId, ct);
                if (!exists)
                {
                    _logger.LogWarning("Pedido {OrderId} não encontrado.", orderId);
                    return;
                }

                _logger.LogInformation("Pedido {OrderId} já não estava Pendente. Nenhuma alteração.", orderId);
            }

            // 2) Simulação de processamento assíncrono
            await Task.Delay(TimeSpan.FromSeconds(FinalizeDelaySeconds), ct);

            await uow.ExecuteInTransactionAsync(async innerCt =>
            {
                var firstTime = await idempotencyStore.TryMarkProcessedAsync(messageId, innerCt);
                if (!firstTime)
                {
                    _logger.LogInformation("Mensagem já processada. MessageId={MessageId}", messageId);
                    return;
                }

                var mudouParaFinalizado = await repo.MarkFinalizedIfProcessingAsync(orderId, innerCt);
                if (mudouParaFinalizado)
                {
                    _logger.LogInformation("Pedido {OrderId} marcado como Finalizado.", orderId);

                    var historyFinalized = OrderStatusHistory.Create(
                        orderId: orderId,
                        fromStatus: orderEnum.OrderStatus.Processando,
                        toStatus:   orderEnum.OrderStatus.Finalizado,
                        correlationId: string.IsNullOrWhiteSpace(correlationId) ? orderId.ToString() : correlationId!,
                        eventId: messageId,
                        source: "Worker",
                        reason: "Status alterado para Finalizado"
                    );

                    await historyRepo.AddAsync(historyFinalized, innerCt);

                }
                else
                {
                    // Outro worker pode ter finalizado; mantém idempotência
                    _logger.LogInformation("Pedido {OrderId} não estava em Processando no momento da finalização.", orderId);
                }
            }, ct);
        }
    }
}
