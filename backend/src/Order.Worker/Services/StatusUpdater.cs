using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Order.Core.Domain;
using Order.Core.Domain.Repositories;
using Order.Core.Domain.Entities.Enums;
using Order.Core.Domain.Entities;


namespace Order.Worker.Services
{
    public sealed class StatusUpdater
    {
        private readonly ILogger<StatusUpdater> _logger;
        private readonly IOrderRepository _repo;

        public StatusUpdater(
            ILogger<StatusUpdater> logger,
            IOrderRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        public async Task AdvanceOrderStatusAsync(Guid orderId, CancellationToken ct)
        {
            // 1. Buscar pedido atual
            var order = await _repo.GetByIdAsync(orderId, ct);
            if (order is null)
            {
                _logger.LogWarning("Pedido {OrderId} não encontrado.", orderId);
                return;
            }

            // 2. Idempotência: se já finalizado, não faz nada
            if (order.Status == OrderStatus.Finalizado)
            {
                _logger.LogInformation(
                    "Pedido {OrderId} já está Finalizado. Ignorando.",
                    orderId
                );
                return;
            }

            // 3. Se está Pendente, marcar como Processando
            if (order.Status == OrderStatus.Pendente)
            {
                order.Status = OrderStatus.Processando;
                await _repo.UpdateAsync(order, ct);

                _logger.LogInformation(
                    "Pedido {OrderId} marcado como Processando.",
                    orderId
                );
            }

            // 4. Esperar 5 segundos simulando processamento
            await Task.Delay(TimeSpan.FromSeconds(5), ct);

            // 5. Buscar novamente (ou reusar tracked, depende do repo)
            order = await _repo.GetByIdAsync(orderId, ct);
            if (order is null)
            {
                _logger.LogWarning(
                    "Pedido {OrderId} não encontrado após Processando.",
                    orderId
                );
                return;
            }

            if (order.Status != OrderStatus.Finalizado)
            {
                order.Status = OrderStatus.Finalizado;
                await _repo.UpdateAsync(order, ct);

                _logger.LogInformation(
                    "Pedido {OrderId} marcado como Finalizado.",
                    orderId
                );
            }
        }
    }
}
