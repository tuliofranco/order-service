using Microsoft.AspNetCore.SignalR;
using Order.Api.Features.Orders.DTOs;

namespace Order.Api.Notification;

public sealed class OrderNotificationHub : Hub<IOrderNotificationClient>
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public async Task BroadcastOrderStatusChanged(Guid orderId)
    {
        await Clients.All.OrderChangeStatusNotification(orderId);
    }
}
