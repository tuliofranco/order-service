using MediatR;
using OrderEntity = Order.Core.Domain.Entities.Order;


namespace Order.Core.Application.Orders.GetAll;
public sealed record GetAllOrdersQuery : IRequest<IReadOnlyList<OrderEntity>>;