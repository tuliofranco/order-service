using System.Net;
using MediatR;
using OrderEntity = Order.Core.Domain.Entities.Order;

namespace Order.Core.Application.Orders.Create;

public sealed record CreateOrderCommand(
    string ClienteNome,
    string Produto,
    decimal Valor
) : IRequest<OrderEntity>;