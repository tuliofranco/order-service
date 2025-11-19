using Order.Core.Domain.Events;
using orderEntity = Order.Core.Domain.Entities.Order;
using Order.Api.Features.Orders.DTOs;

namespace Order.Api.Notification;
public interface IOrderNotificationClient
{
    Task OrderCreatedNotification(OrderResponse orderEvent);
    Task OrderChangeStatusNotification(Guid orderId);
}