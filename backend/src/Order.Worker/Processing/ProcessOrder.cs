using Order.Core.Application.Abstractions;
using Order.Core.Application.Abstractions.Idempotency;
using Order.Core.Application.Abstractions.Repositories;
using Order.Core.Domain.Entities;
using Order.Infrastructure.Persistence;
using Order.Core.Domain.Entities.Enums;

namespace Order.Worker.Processing;

public sealed class ProcessOrder
{
    private readonly ILogger<ProcessOrder> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private const int FinalizeDelaySeconds = 5;

    private static OrderStatusHistory BuildHistory(
        Guid orderId,
        OrderStatus from,
        OrderStatus to,
        string messageId,
        string? correlationId,
        string reason)
    {
        return OrderStatusHistory.Create(
            orderId: orderId,
            fromStatus: from,
            toStatus: to,
            correlationId: string.IsNullOrWhiteSpace(correlationId) ? orderId.ToString() : correlationId!,
            eventId: messageId,
            source: "Worker",
            reason: reason
        );
    }

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
            using var scope      = _scopeFactory.CreateScope();
            var db               = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var repo             = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var historyRepo      = scope.ServiceProvider.GetRequiredService<IOrderStatusHistoryRepository>();
            var idempotencyStore = scope.ServiceProvider.GetRequiredService<IProcessedMessageStore>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await uow.ExecuteInTransactionAsync(async innerCt =>
            {
                bool pendingToProcessing = await repo.ChangeStatusAsync(orderId, OrderStatus.Pendente, OrderStatus.Processando, innerCt);
                if (pendingToProcessing)
                {
                    var historyProcessed = BuildHistory(
                        orderId,
                        OrderStatus.Pendente,
                        OrderStatus.Processando,
                        messageId,
                        correlationId,
                        "Status alterado para Processando"
                    );
                    await historyRepo.AddAsync(historyProcessed, innerCt);

                    _logger.LogInformation("Pedido {OrderId} marcado como Processando.", orderId);
                }
                else
                {
                    bool exists = await repo.ExistsAsync(orderId, innerCt);
                    if (!exists)
                    {
                        _logger.LogWarning("Pedido {OrderId} não encontrado.", orderId);
                        return;
                    }
                    _logger.LogInformation("Pedido {OrderId} já não estava Pendente. Nenhuma alteração.", orderId);
                }
            }, ct);

            // 2) Simulação de processamento assíncrono
            _logger.LogInformation("Simulando processamento assincrono de 5 segundos.");
            await Task.Delay(TimeSpan.FromSeconds(FinalizeDelaySeconds), ct);
            _logger.LogInformation("Processamento finalizado.");


            await uow.ExecuteInTransactionAsync(async innerCt =>
            {
                bool firstTime = await idempotencyStore.TryMarkProcessedAsync(messageId, innerCt);
                if (!firstTime)
                {
                    _logger.LogInformation("Mensagem já processada. MessageId={MessageId}", messageId);
                    return;
                }

                bool processingToCompleted = await repo.ChangeStatusAsync(orderId, OrderStatus.Processando, OrderStatus.Finalizado, innerCt);
                if (processingToCompleted)
                {
                    var historyFinalized = BuildHistory(
                        orderId,
                        OrderStatus.Processando,
                        OrderStatus.Finalizado,
                        messageId,
                        correlationId,
                        "Status alterado para Finalizado"
                    );
                    await historyRepo.AddAsync(historyFinalized, innerCt);

                    _logger.LogInformation("Pedido {OrderId} marcado como Finalizado.", orderId);
                }
                else
                {
                    _logger.LogInformation("Pedido {OrderId} não estava em Processando no momento da finalização.", orderId);
                }
            }, ct);
        }
    }
}
