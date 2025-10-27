#nullable enable
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;  

namespace Order.Infrastructure.HealthChecks;

public sealed class ServiceBusHealthCheck : IHealthCheck
{
    private readonly ServiceBusClient _client;
    private readonly string _queue;

    public ServiceBusHealthCheck(ServiceBusClient client, IConfiguration cfg)
    {
        _client = client;
        _queue = cfg["ServiceBus:QueueName"] ?? "orders";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica se conseguimos criar um sender (sem enviar nada)
            await using var sender = _client.CreateSender(_queue);
            _ = sender; // apenas para satisfazer o compilador
            return HealthCheckResult.Healthy($"ServiceBus OK (queue: {_queue})");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ServiceBus indisponível", ex);
        }
    }
}
