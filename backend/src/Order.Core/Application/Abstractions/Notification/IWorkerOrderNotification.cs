namespace Order.Core.Application.Abstractions.Notification;

public interface IWorkerOrderNotification
{
    Task NotifyOrderStatusChangedAsync(Guid order, CancellationToken ct = default);
}
