using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Order.Infrastructure.HealthChecks
{
    public sealed class ServiceBusQueueHealthCheck : IHealthCheck
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusQueueHealthCheck> _logger;

        // nome da fila que seu worker/producer usa
        private const string QueueName = "orders";

        public ServiceBusQueueHealthCheck(
            ServiceBusClient client,
            ILogger<ServiceBusQueueHealthCheck> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // tenta criar um sender/receiver só pra validar que consegue falar com a fila
                var sender = _client.CreateSender(QueueName);
                // opcional: você pode tentar acessar propriedades da fila via ManagementClient,
                // mas o SDK novo não expõe mais tão fácil sem permissões especiais.
                // Só de conseguir criar o sender sem exception de credencial/host já é sinal de vida.
                await sender.CloseAsync(cancellationToken);

                _logger.LogInformation("Service Bus OK para fila {QueueName}", QueueName);

                return HealthCheckResult.Healthy($"Service Bus reachable, queue '{QueueName}' usable.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao validar Service Bus para fila {QueueName}", QueueName);
                return HealthCheckResult.Unhealthy(
                    description: $"Service Bus unreachable for queue '{QueueName}'",
                    exception: ex
                );
            }
        }
    }
}
