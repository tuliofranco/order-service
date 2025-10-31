using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Order.Infrastructure.HealthChecks
{
    public sealed class ServiceBusQueueHealthCheck : IHealthCheck
    {
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusQueueHealthCheck> _logger;
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
                var sender = _client.CreateSender(QueueName);
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
