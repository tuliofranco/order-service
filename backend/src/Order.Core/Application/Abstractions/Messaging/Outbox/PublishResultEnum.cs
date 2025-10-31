namespace Order.Core.Application.Abstractions.Messaging.Outbox;


public enum PublishResult
{
    Success,
    RetryableFailure,
    PermanentFailure
}
