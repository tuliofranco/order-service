using OrderEntity = Order.Core.Domain.Entities.Order;
using Order.Core.Application.Abstractions.Repositories;
using Order.Core.Application.Abstractions;
using Order.Core.Application.Abstractions.Messaging.Outbox;
using Order.Core.Domain.Rules;
using Order.Core.Domain.Entities.Enums;
using Order.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using Order.Core.Domain.Events;

namespace Order.Core.Application.Services;

public class OrderService(
    IOrderRepository repo,
    IUnitOfWork uow,
    IOrderStatusHistoryRepository historyRepo,
    IOutboxStore outbox,
    ILogger<OrderService> logger
) : IOrderService
{
    private readonly IOrderRepository _repo = repo;
    private readonly IUnitOfWork _uow = uow;
    private readonly IOrderStatusHistoryRepository _historyRepo = historyRepo;
    private readonly IOutboxStore _outbox = outbox;
    private readonly ILogger<OrderService> _logger = logger;

    public async Task<OrderEntity> CreateOrderAsync(
        string clienteNome,
        string produto,
        decimal valor,
        CancellationToken ct = default)
    {
        var order = OrderEntity.Create(clienteNome, produto, valor);
        var correlationId = order.Id.ToString();

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["orderId"] = order.Id,
            ["correlationId"] = correlationId
        }))
        {
            _logger.LogInformation("CreateOrder start");

            OrderStatusTransitionValidator.EnsureValid(null, OrderStatus.Pendente);

            var integrationEvent = new OrderCreatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: DateTime.UtcNow,
                Type: "OrderCreated", // simples pro desafio ✅
                CorrelationId: correlationId,
                CausationId: null,
                OrderId: order.Id,
                Cliente: clienteNome,
                Produto: produto,
                Valor: valor
            );

            await _repo.AddAsync(order, ct);

            var historyEntry = OrderStatusHistory.Create(
                orderId: order.Id,
                fromStatus: null,
                toStatus: OrderStatus.Pendente,
                correlationId: correlationId,
                eventId: integrationEvent.Id.ToString(),
                source: "Api",
                reason: "Pedido criado"
            );

            await _historyRepo.AddAsync(historyEntry, ct);

            _logger.LogInformation("Outbox enqueue {EventId}", integrationEvent.Id);
            await _outbox.AppendAsync(integrationEvent, ct);

            await _uow.CommitAsync(ct);

            _logger.LogInformation("CreateOrder commit ok");

            return order;
        }
    }

    public Task<IReadOnlyList<OrderEntity>> GetAllAsync(CancellationToken ct = default)
        => _repo.GetAllAsync(ct);

    public Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<OrderStatusHistory>> GetHistoryByOrderIdAsync(
        Guid orderId,
        CancellationToken ct = default)
        => _historyRepo.GetByOrderIdAsync(orderId, ct);
}
