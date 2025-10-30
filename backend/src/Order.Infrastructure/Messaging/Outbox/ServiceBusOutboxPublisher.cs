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

            _logger.LogInformation(
                "Mensagem publicada no Service Bus. OutboxId={OutboxId} Tipo={Type}",
                record.Id,
                record.Type
            );

            return PublishResult.Success;
        }
        catch (ServiceBusException sbEx) when (IsPermanent(sbEx))
        {
            _logger.LogWarning(
                "Falha permanente ao publicar no Service Bus. Motivo={Reason} Erro={Message}",
                sbEx.Reason,
                sbEx.Message
            );

            return PublishResult.PermanentFailure;
        }
        catch (ServiceBusException sbEx)
        {
            _logger.LogWarning(
                "Falha temporária ao publicar no Service Bus. Motivo={Reason} Erro={Message}",
                sbEx.Reason,
                sbEx.Message
            );

            // Considerar retry
            return PublishResult.RetryableFailure;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Erro inesperado ao publicar no Service Bus. Erro={Message}",
                ex.Message
            );

            // Considerar retry
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
