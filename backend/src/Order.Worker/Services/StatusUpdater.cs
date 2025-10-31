using Order.Core.Application.Abstractions.Repositories;
using Order.Core.Domain.Entities.Enums;


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
            var order = await _repo.GetByIdAsync(orderId, ct);
            if (order is null)
            {
                _logger.LogWarning("Pedido {OrderId} não encontrado.", orderId);
                return;
            }
            if (order.Status == OrderStatus.Finalizado)
            {
                _logger.LogInformation(
                    "Pedido {OrderId} já está Finalizado. Ignorando.",
                    orderId
                );
                return;
            }

            if (order.Status == OrderStatus.Pendente)
            {
                order.Status = OrderStatus.Processando;
                await _repo.UpdateAsync(order, ct);

                _logger.LogInformation(
                    "Pedido {OrderId} marcado como Processando.",
                    orderId
                );
            }

            // Requisito do desafio para simular o comportamento assincrono
            // ( "Criar um worker que consome mensagens, atualiza para 'Processando' e, após 5 segundos, altera para 'Finalizado'" )
            await Task.Delay(TimeSpan.FromSeconds(5), ct);

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
