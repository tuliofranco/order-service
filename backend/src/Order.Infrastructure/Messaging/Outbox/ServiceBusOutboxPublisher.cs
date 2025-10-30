using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Order.Core.Abstractions.Messaging.Outbox;
using Microsoft.Extensions.Logging;

namespace Order.Infrastructure.Messaging.Outbox;

public sealed class ServiceBusOutboxPublisher : IOutboxPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusOutboxPublisher> _logger;

    private readonly string _entityName;
    public ServiceBusOutboxPublisher(ServiceBusClient client, string entityName, ILogger<ServiceBusOutboxPublisher> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _entityName = string.IsNullOrWhiteSpace(entityName)
            ? throw new ArgumentException("Entity name must be provided.", nameof(entityName))
            : entityName;
        _logger = logger;
    }

    public async Task<PublishResult> PublishAsync(OutboxRecord record, CancellationToken ct = default)
    {
        var message = new ServiceBusMessage(BinaryData.FromString(record.Payload))
        {
            ContentType = "application/json",
            MessageId = record.Id.ToString(),
            Subject = record.Type,
        };

        message.ApplicationProperties["type"] = record.Type;
        message.ApplicationProperties["occurredOnUtc"] = record.OccurredOnUtc;

        try
        {
            var sender = _client.CreateSender(_entityName);
            await sender.SendMessageAsync(message, ct);
            return PublishResult.Success;
        }
        catch (ServiceBusException sbEx) when (IsPermanent(sbEx))
        {
            _logger.LogWarning($"[OutboxPublisher] Permanent failure: {sbEx.Reason} - {sbEx.Message}");
            return PublishResult.PermanentFailure;
        }
        catch (ServiceBusException sbEx)
        {
            // Transient ou não mapeado explicitamente -> tentar novamente
            _logger.LogWarning($"[OutboxPublisher] Retryable failure: {sbEx.Reason} - {sbEx.Message}");
            return PublishResult.RetryableFailure;
        }
        catch (Exception ex)
        {
            // Falhas desconhecidas: considere retry (no desafio é aceitável)
            _logger.LogWarning($"[OutboxPublisher] Unexpected error: {ex.Message}");
            return PublishResult.RetryableFailure;
        }
    }

    private static bool IsPermanent(ServiceBusException ex)
    {
        return ex.Reason switch
        {
            ServiceBusFailureReason.MessagingEntityDisabled => true,
            ServiceBusFailureReason.MessagingEntityNotFound => true,
            ServiceBusFailureReason.MessageSizeExceeded => true,
            ServiceBusFailureReason.QuotaExceeded => true,
            _ => false
        };
    }
}
