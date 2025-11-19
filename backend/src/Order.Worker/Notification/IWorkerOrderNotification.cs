using Order.Api.Features.Orders.DTOs;

namespace Order.Worker.Notification;

public interface IWorkerOrderNotification
{
    Task NotifyOrderStatusChangedAsync(Guid order, CancellationToken ct = default);
}
