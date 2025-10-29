#nullable enable

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;


namespace Order.Infrastructure.Messaging;
public sealed class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusPublisher> _logger;

    public ServiceBusPublisher(
        ServiceBusClient client,
        ILogger<ServiceBusPublisher> logger)
    {
        _client = client;
        _logger = logger;

        _sender = _client.CreateSender("orders");
    }

    public async Task PublishOrderCreatedAsync(
        Guid orderId,
        string eventType,
        Guid correlationId,
        CancellationToken ct = default)
    {
        var bodyJson = JsonSerializer.Serialize(new
        {
            OrderId = orderId
        });

        var message = new ServiceBusMessage(bodyJson)
        {
            MessageId = orderId.ToString(),
            CorrelationId = correlationId.ToString(),
            ContentType = "application/json",
        };

        message.ApplicationProperties["EventType"] = eventType;
        message.ApplicationProperties["OrderId"] = orderId.ToString();

        await _sender.SendMessageAsync(message, ct);

        _logger.LogInformation(
            "Mensagem {EventType} publicada. OrderId={OrderId} CorrelationId={CorrelationId} MessageId={MessageId}",
            eventType,
            orderId,
            correlationId,
            message.MessageId);
    }
    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}

