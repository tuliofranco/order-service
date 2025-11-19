using Order.Api.Features.Orders.DTOs;


namespace Order.Api.Notification;

public interface IOrderNotificationService
{
    Task NotifyOrderCreatedAsync(OrderResponse evt, CancellationToken ct = default);
    Task NotifyOrderStatusChangedAsync(Guid orderId, CancellationToken ct = default);
}
