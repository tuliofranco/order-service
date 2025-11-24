using MediatR;
using Order.Core.Domain.Entities;
using OrderEntity  = Order.Core.Domain.Entities.Order;
using OrderHistory = Order.Core.Domain.Entities;

namespace Order.Core.Application.Orders.GetDetails;

public sealed record OrderDetailsResult(
    OrderEntity Order,
    IReadOnlyList<OrderStatusHistory> History
);

public sealed record GetOrderDetailsQuery(Guid Id)
    : IRequest<GetOrderDetailsResult?>;
