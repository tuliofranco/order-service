using Order.Core.Domain.Entities;
using OrderEntity = Order.Core.Domain.Entities.Order;

namespace Order.Core.Application.Orders.GetDetails;

public sealed record GetOrderDetailsResult(
    OrderEntity Order,
    IReadOnlyList<OrderStatusHistory> History
);