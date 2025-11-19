using Microsoft.AspNetCore.SignalR;
using Order.Api.Features.Orders.DTOs;

namespace Order.Api.Notification;

public sealed class OrderNotificationService : IOrderNotificationService
{
    private IHubContext<OrderNotificationHub, IOrderNotificationClient> _hubContext;
    public OrderNotificationService(IHubContext<OrderNotificationHub, IOrderNotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }
    public Task NotifyOrderCreatedAsync(OrderResponse orderEvent, CancellationToken ct = default)
    {
        return _hubContext.Clients.All.OrderCreatedNotification(orderEvent);
    }

    public Task NotifyOrderStatusChangedAsync(Guid orderId,  CancellationToken ct = default)
    {
        return _hubContext.Clients.All.OrderChangeStatusNotification(orderId);
    }

}